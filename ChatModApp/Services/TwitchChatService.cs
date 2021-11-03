using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using ChatModApp.Models;
using ChatModApp.Models.Chat;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace ChatModApp.Services;

public class TwitchChatService : IDisposable
{
    public IObservable<IChangeSet<ITwitchChannel>> ChannelsJoined { get; }
    public IObservableList<TwitchChatBadge> ChatBadges { get; }
    public IObservable<ChatMessage> ChatMessageReceived { get; }
    public IObservable<SentMessage> ChatMessageSent { get; }

    private readonly TwitchApiService _apiService;
    private readonly AuthenticationService _authService;
    private readonly TwitchClient _client;
    private readonly CompositeDisposable _disposables;

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

        apiService.UserConnected.Subscribe(Connect).DisposeWith(_disposables);

        var globalBadges = Observable.FromEventPattern<OnConnectedArgs>(_client, nameof(_client.OnConnected))
                                     .Select(_ => GetGlobalChatBadges().ToObservable()).Concat().ToObservableChangeSet();

        ChatMessageReceived = Observable.FromEventPattern<OnMessageReceivedArgs>(_client, nameof(_client.OnMessageReceived))
                                        .ObserveOn(RxApp.TaskpoolScheduler).Select(pattern => pattern.EventArgs.ChatMessage);

        ChatMessageSent = Observable.FromEventPattern<OnMessageSentArgs>(_client, nameof(_client.OnMessageSent))
                                    .ObserveOn(RxApp.TaskpoolScheduler).Select(pattern => pattern.EventArgs.SentMessage);

        ChannelsJoined = tabService.Tabs.AutoRefresh(item => item.Channel).Filter(channel => channel.Channel is not null)
                                   .DistinctValues(item => item.Channel);

        ChatBadges = ChannelsJoined.TransformAsync(GetChannelChatBadges).TransformMany(badges => badges).Or(globalBadges)
                                   .AsObservableList();
        ChannelsJoined.OnItemAdded(channel => _client.JoinChannel(channel.Login))
                      .OnItemRemoved(channel => _client.LeaveChannel(channel.Login)).Subscribe().DisposeWith(_disposables);

        ChatBadges.DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void SendMessage(ITwitchChannel channel, string message)
    {
        _client.SendMessage(channel.Login, message);
    }

    private void Connect(User user)
    {
        _client.Initialize(new(user.Login, _authService.TwitchAccessToken));
        _client.Connect();
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