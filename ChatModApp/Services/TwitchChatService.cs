using System;
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
    public class TwitchChatService
    {
        public IObservableList<string> ChannelsJoined { get; }
        public IObservable<ChatMessage> ChatMessageReceived { get; }

        private readonly AuthenticationService _authService;
        private readonly TwitchClient _client;

        private readonly SourceList<string> _joinedChannels;

        public TwitchChatService(TwitchApiService apiService, AuthenticationService authService,
                                 ILogger<TwitchClient> clientLogger)
        {
            _authService = authService;

            _joinedChannels = new SourceList<string>();

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 10000,
                ThrottlingPeriod = TimeSpan.FromSeconds(1)
            };

            var customSocketClient = new WebSocketClient(clientOptions);

            _client = new TwitchClient(customSocketClient, logger: clientLogger);

            apiService.UserConnected
                      .Subscribe(Connect);

            ChannelsJoined = _joinedChannels.Connect()
                                            .AsObservableList();

            ChatMessageReceived = Observable
                                  .FromEventPattern<OnMessageReceivedArgs>(_client, nameof(_client.OnMessageReceived))
                                  .Select(pattern => pattern.EventArgs.ChatMessage);
        }

        public void JoinChannel(string channel)
        {
            _client.JoinChannel(channel);
            _joinedChannels.Add(channel);
        }

        public void LeaveChannel(string channel)
        {
            _client.LeaveChannel(channel);
            _joinedChannels.Remove(channel);
        }


        private void Connect(User user)
        {
            _client.Initialize(new ConnectionCredentials(user.Login, _authService.TwitchAccessToken));
            _client.Connect();
        }
    }
}