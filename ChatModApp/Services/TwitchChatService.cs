using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using ChatModApp.Models.Chat;
using DynamicData;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace ChatModApp.Services
{
    public class TwitchChatService : IDisposable
    {
        public IObservableList<string> ChannelsJoined { get; }
        public IObservableList<TwitchChatBadge> ChatBadges { get; }
        public IObservable<ChatMessage> ChatMessageReceived { get; }
        public IObservable<SentMessage> ChatMessageSent { get; }


        private readonly TwitchApiService _apiService;
        private readonly AuthenticationService _authService;
        private readonly TwitchClient _client;

        private readonly CompositeDisposable _disposables;

        public TwitchChatService(TwitchApiService apiService, AuthenticationService authService,
                                 ILogger<TwitchClient> clientLogger)
        {
            var joinedChannels = new SourceList<string>();
            _disposables = new CompositeDisposable();
            _apiService = apiService;
            _authService = authService;


            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 10000,
                ThrottlingPeriod = TimeSpan.FromSeconds(1)
            };
            _client = new TwitchClient(new WebSocketClient(clientOptions), logger: clientLogger);

            apiService.UserConnected
                      .Subscribe(Connect)
                      .DisposeWith(_disposables);

            var globalBadges = Observable.FromEventPattern<OnConnectedArgs>(_client, nameof(_client.OnConnected))
                                         .Select(_ => GetGlobalChatBadges().ToObservable())
                                         .Concat()
                                         .ToObservableChangeSet();

            ChannelsJoined = joinedChannels.Connect()
                                           .AsObservableList();

            ChatMessageReceived = Observable
                                  .FromEventPattern<OnMessageReceivedArgs>(_client, nameof(_client.OnMessageReceived))
                                  .Select(pattern => pattern.EventArgs.ChatMessage);

            ChatMessageSent = Observable
                              .FromEventPattern<OnMessageSentArgs>(_client, nameof(_client.OnMessageSent))
                              .Select(pattern => pattern.EventArgs.SentMessage);

            Observable.FromEventPattern<OnJoinedChannelArgs>(_client, nameof(_client.OnJoinedChannel))
                      .Select(pattern => pattern.EventArgs.Channel)
                      .Subscribe(s => joinedChannels.Add(s))
                      .DisposeWith(_disposables);

            Observable.FromEventPattern<OnLeftChannelArgs>(_client, nameof(_client.OnLeftChannel))
                      .Select(pattern => pattern.EventArgs.Channel)
                      .Subscribe(s => joinedChannels.Remove(s))
                      .DisposeWith(_disposables);

            ChatBadges = ChannelsJoined.Connect()
                                       .TransformAsync(GetChannelChatBadges)
                                       .TransformMany(badges => badges)
                                       .Or(globalBadges)
                                       .AsObservableList();

            joinedChannels.DisposeWith(_disposables);
            ChatBadges.DisposeWith(_disposables);
            ChannelsJoined.DisposeWith(_disposables);
        }

        public void JoinChannel(string channel) => _client.JoinChannel(channel);
        public void LeaveChannel(string channel) => _client.LeaveChannel(channel);

        public void SendMessage(string channel, string message) => _client.SendMessage(channel, message);

        public void Dispose() => _disposables.Dispose();


        private void Connect(User user)
        {
            _client.Initialize(new ConnectionCredentials(user.Login, _authService.TwitchAccessToken));
            _client.Connect();
        }

        private async Task<IEnumerable<TwitchChatBadge>> GetChannelChatBadges(string channel)
        {
            var res1 = await _apiService.Helix.Users.GetUsersAsync(logins: new List<string> {channel});
            var user = res1.Users.Single();

            var res2 = await _apiService.Helix.Chat.GetChannelChatBadgesAsync(user.Id);

            return from emoteSet in res2.EmoteSet
                   from version in emoteSet.Versions
                   select new TwitchChatBadge(channel,
                                              emoteSet.SetId,
                                              version.Id,
                                              new Uri(version.ImageUrl1x),
                                              new Uri(version.ImageUrl2x),
                                              new Uri(version.ImageUrl4x));
        }

        private async Task<IEnumerable<TwitchChatBadge>> GetGlobalChatBadges()
        {
            var res = await _apiService.Helix.Chat.GetGlobalChatBadgesAsync();

            return from emoteSet in res.EmoteSet
                   from version in emoteSet.Versions
                   select new TwitchChatBadge(string.Empty,
                                              emoteSet.SetId,
                                              version.Id,
                                              new Uri(version.ImageUrl1x),
                                              new Uri(version.ImageUrl2x),
                                              new Uri(version.ImageUrl4x));
        }
    }
}