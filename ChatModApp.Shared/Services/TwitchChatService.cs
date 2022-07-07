using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Services.ApiClients;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace ChatModApp.Shared.Services;

public class TwitchChatService : IDisposable
{
    public IObservable<IChangeSet<ITwitchChannel>> ChannelsJoined { get; }
    public IObservableList<TwitchChatBadge> ChatBadges { get; }
    public IObservable<ChatMessage> ChatMessageReceived { get; }
    public IObservable<SentMessage> ChatMessageSent { get; }


    private readonly TwitchApiService _apiService;
    private readonly AuthenticationService _authService;
    private readonly IRobottyApi _historyApi;
    private readonly WebSocketClient _client;
    private readonly TwitchClient _twitchClient;

    private readonly CompositeDisposable _disposables;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem")]
    public TwitchChatService(TwitchApiService apiService,
                             AuthenticationService authService,
                             ChatTabService tabService,
                             IRobottyApi historyApi,
                             ILogger<TwitchClient> clientLogger)
    {
        _disposables = new();
        _apiService = apiService;
        _authService = authService;
        _historyApi = historyApi;

        _client = new(new ClientOptions
        {
            MessagesAllowedInPeriod = 10000,
            ThrottlingPeriod = TimeSpan.FromSeconds(1)
        });
        _twitchClient = new(_client, logger: clientLogger);

        apiService.UserConnected
                  .Subscribe(Connect)
                  .DisposeWith(_disposables);

        var globalBadges = Observable
                           .FromEventPattern<OnConnectedArgs>(_twitchClient, nameof(_twitchClient.OnConnected))
                           .Select(_ => GetGlobalChatBadges().ToObservable())
                           .Concat()
                           .ToObservableChangeSet();

        ChatMessageReceived = Observable
                              .FromEventPattern<OnMessageReceivedArgs>(_twitchClient,
                                                                       nameof(_twitchClient.OnMessageReceived))
                              .ObserveOn(RxApp.TaskpoolScheduler)
                              .Select(pattern => pattern.EventArgs.ChatMessage);

        ChatMessageSent = Observable
                          .FromEventPattern<OnMessageSentArgs>(_twitchClient, nameof(_twitchClient.OnMessageSent))
                          .ObserveOn(RxApp.TaskpoolScheduler)
                          .Select(pattern => pattern.EventArgs.SentMessage);

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
            .OnItemAdded(channel => _twitchClient.JoinChannel(channel.Login))
            .OnItemRemoved(channel => _twitchClient.LeaveChannel(channel.Login))
            .Subscribe()
            .DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();

    public void SendMessage(ITwitchChannel channel, string message) =>
        _twitchClient.SendMessage(channel.Login, message);

    public async Task LoadMessageHistory(ITwitchChannel channel, CancellationToken cancellationToken = default)
    {
        var res = await _historyApi.GetRecentMessages(channel.Login, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            return;
        }

        var messages = res.Content?.Messages ?? ImmutableArray<string>.Empty;

        foreach (var msg in messages)
        {
            _client.RaiseEvent(nameof(_client.OnMessage), new OnMessageEventArgs { Message = msg });
        }
    }

    public async Task<IEnumerable<ChatterFormatted>> GetChatUserList(ITwitchChannel channel)
    {
        return await _apiService.Undocumented.GetChattersAsync(channel.Login);
    }


    private void Connect(User user)
    {
        _twitchClient.Initialize(new(user.Login, _authService.TwitchAccessToken));
        _twitchClient.Connect();
    }

    private async Task<IEnumerable<TwitchChatBadge>> GetChannelChatBadges(ITwitchChannel channel)
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