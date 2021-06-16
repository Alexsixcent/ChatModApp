using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
        public IObservable<ChatMessage> ChatMessageReceived { get; }

        private readonly AuthenticationService _authService;
        private readonly TwitchClient _client;

        private readonly CompositeDisposable _disposables;

        public TwitchChatService(TwitchApiService apiService, AuthenticationService authService,
                                 ILogger<TwitchClient> clientLogger)
        {
            var joinedChannels = new SourceList<string>();
            _authService = authService;

            _disposables = new CompositeDisposable();

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 10000,
                ThrottlingPeriod = TimeSpan.FromSeconds(1)
            };

            var customSocketClient = new WebSocketClient(clientOptions);

            _client = new TwitchClient(customSocketClient, logger: clientLogger);

            apiService.UserConnected
                      .Subscribe(Connect);

            ChannelsJoined = joinedChannels.Connect()
                                            .AsObservableList();

            ChatMessageReceived = Observable
                              .FromEventPattern<OnMessageReceivedArgs>(_client, nameof(_client.OnMessageReceived))
                              .Select(pattern => pattern.EventArgs.ChatMessage);

            Observable.FromEventPattern<OnJoinedChannelArgs>(_client, nameof(_client.OnJoinedChannel))
                      .Select(pattern => pattern.EventArgs.Channel)
                      .Subscribe(s => joinedChannels.Add(s))
                      .DisposeWith(_disposables);

            Observable.FromEventPattern<OnLeftChannelArgs>(_client, nameof(_client.OnLeftChannel))
                      .Select(pattern => pattern.EventArgs.Channel)
                      .Subscribe(s => joinedChannels.Remove(s))
                      .DisposeWith(_disposables);

            joinedChannels.DisposeWith(_disposables);
        }

        public void JoinChannel(string channel) => _client.JoinChannel(channel);

        public void LeaveChannel(string channel) => _client.LeaveChannel(channel);

        public void Dispose() => _disposables.Dispose();


        private void Connect(User user)
        {
            _client.Initialize(new ConnectionCredentials(user.Login, _authService.TwitchAccessToken));
            _client.Connect();
        }
    }
}