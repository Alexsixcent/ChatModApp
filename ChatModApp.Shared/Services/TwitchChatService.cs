using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace ChatModApp.Shared.Services;

public sealed class TwitchChatService : IDisposable
{
    public IObservable<IChangeSet<ITwitchUser>> ChannelsJoined { get; }
    public IObservableList<TwitchChatBadge> ChatBadges { get; }
    public IObservable<ChatMessage> ChatMessageReceived { get; }
    public IObservable<SentMessage> ChatMessageSent { get; }
    public IObservable<Subscriber> ChatNewSub { get; }
    public IObservable<OnGiftedSubscriptionArgs> ChatGiftedSubs { get; }
    public IObservable<ReSubscriber> ChatResubbed { get; }


    private readonly TwitchApiService _apiService;
    private readonly AuthenticationService _authService;
    private readonly TwitchClient _client;

    private readonly CompositeDisposable _disposables;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public TwitchChatService(TwitchApiService apiService,
                             AuthenticationService authService,
                             ChatTabService tabService,
                             ILogger<TwitchClient> clientLogger)
    {
        _disposables = new();
        _apiService = apiService;
        _authService = authService;

        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 10000,
            ThrottlingPeriod = TimeSpan.FromSeconds(1)
        };
        _client = new(new WebSocketClient(clientOptions), logger: clientLogger);

        apiService.UserConnected
                  .Subscribe(Connect)
                  .DisposeWith(_disposables);

        var globalBadges = Observable.FromEventPattern<OnConnectedArgs>(_client, nameof(_client.OnConnected))
                                     .Select(_ => GetGlobalChatBadges().ToObservable())
                                     .Concat()
                                     .ToObservableChangeSet();

        ChatMessageReceived = Observable
                              .FromEventPattern<OnMessageReceivedArgs>(_client, nameof(_client.OnMessageReceived))
                              .ObserveOnThreadPool()
                              .Select(pattern => pattern.EventArgs.ChatMessage);

        ChatMessageSent = Observable
                          .FromEventPattern<OnMessageSentArgs>(_client, nameof(_client.OnMessageSent))
                          .ObserveOnThreadPool()
                          .Select(pattern => pattern.EventArgs.SentMessage);

        ChatNewSub = Observable.FromEventPattern<OnNewSubscriberArgs>(_client, nameof(_client.OnNewSubscriber))
                  .ObserveOnThreadPool()
                  .Select(pattern => pattern.EventArgs.Subscriber);

        ChatResubbed = Observable.FromEventPattern<OnReSubscriberArgs>(_client, nameof(_client.OnReSubscriber))
                  .ObserveOnThreadPool()
                  .Select(pattern => pattern.EventArgs.ReSubscriber);

        ChatGiftedSubs = Observable.FromEventPattern<OnGiftedSubscriptionArgs>(_client, nameof(_client.OnGiftedSubscription))
                  .ObserveOnThreadPool()
                  .Select(pattern => pattern.EventArgs);
        
        ChannelsJoined = tabService.Tabs
                                   .AutoRefresh(item => item.Channel)
                                   .Filter(channel => channel.Channel is not null)
                                   .DistinctValues(item => item.Channel);

        ChatBadges = ChannelsJoined.TransformAsync(GetChannelChatBadges)
                                   .TransformMany(badges => badges)
                                   .Or(globalBadges)
                                   .AsObservableList()
                                   .DisposeWith(_disposables);
        ChannelsJoined
            .OnItemAdded(channel => _client.JoinChannel(channel.Login))
            .OnItemRemoved(channel => _client.LeaveChannel(channel.Login))
            .Subscribe()
            .DisposeWith(_disposables);
    }

    public void SendMessage(ITwitchUser user, string message) => _client.SendMessage(user.Login, message);

    public async Task<IEnumerable<TwitchChatter>> GetChatUserList(ITwitchUser channel)
    {
        var user = _apiService.CurrentUser;
        if (user is null) return Enumerable.Empty<TwitchChatter>();

        var chatters = new Dictionary<string, TwitchChatter>();
        string? page = null;
        do
        {
            var res = await _apiService.Helix.Chat.GetChattersAsync(channel.Id, user.Id, 1000, page);
            page = res.Pagination.Cursor;

            foreach (var c in res.Data)
            {
                chatters.Add(c.UserId, new(UserType.Viewer, c.UserId, c.UserName));
            }
        } while (page is not null);
        
        if (chatters.Remove(channel.Id))
        {
            chatters.Add(channel.Id, new(UserType.Broadcaster, channel.Id, channel.DisplayName));
        }
        
        if (user.Id != channel.Id) return chatters.Values;
        
        var moderators = new List<TwitchChatter>();
        do
        {
            var res = await _apiService.Helix.Moderation.GetModeratorsAsync(user.Id, first: 100, after: page);
            page = res.Pagination.Cursor;
            moderators.AddRange(res.Data.Select(m => new TwitchChatter(UserType.Moderator, m.UserId, m.UserName)));
        } while (page is not null);

        var vips = new List<TwitchChatter>();
        do
        {
            var res = await _apiService.Helix.Channels.GetVIPsAsync(user.Id, first: 100, after: page);
            page = res.Pagination.Cursor;
            vips.AddRange(res.Data.Select(v => new TwitchChatter(UserType.VIP, v.UserId, v.UserName)));
        } while (page is not null);

        foreach (var vip in vips.Where(vip => chatters.Remove(vip.Id)))
        {
            chatters.Add(vip.Id, vip);
        }

        foreach (var mod in moderators.Where(mod => chatters.Remove(mod.Id)))
        {
            chatters.Add(mod.Id, mod);
        }

        return chatters.Values;
    }

    public void Dispose() => _disposables.Dispose();


    private void Connect(User user)
    {
        _client.Initialize(new(user.Login, _authService.TwitchAccessToken));
        _client.Connect();
    }

    private async Task<IEnumerable<TwitchChatBadge>> GetChannelChatBadges(ITwitchUser channel)
    {
        var res1 = await _apiService.Helix.Users.GetUsersAsync(logins: new() { channel.Login });
        var user = res1.Users.Single();

        var res2 = await _apiService.Helix.Chat.GetChannelChatBadgesAsync(user.Id);

        return from emoteSet in res2.EmoteSet
               from version in emoteSet.Versions
               select new TwitchChatBadge(channel,
                                          emoteSet.SetId,
                                          version.Id,
                                          new(version.ImageUrl1x),
                                          new(version.ImageUrl2x),
                                          new(version.ImageUrl4x));
    }

    private async Task<IEnumerable<TwitchChatBadge>> GetGlobalChatBadges()
    {
        var res = await _apiService.Helix.Chat.GetGlobalChatBadgesAsync();

        return from emoteSet in res.EmoteSet
               from version in emoteSet.Versions
               select new TwitchChatBadge(emoteSet.SetId,
                                          version.Id,
                                          new(version.ImageUrl1x),
                                          new(version.ImageUrl2x),
                                          new(version.ImageUrl4x));
    }
}

public record TwitchChatter(UserType Type, string Id, string DisplayName);