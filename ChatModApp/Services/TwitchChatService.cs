using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using Tools;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace ChatModApp.Services
{
    public class TwitchChatService : IService
    {
        public IObservableList<string> ChannelsJoined { get; }
        public IObservable<ChatMessage> ChatMessageReceived { get; }

        private readonly AuthenticationService _authService;
        private readonly EmotesService _emotesService;
        private readonly TwitchClient _client;

        private readonly SourceList<string> _joinedChannels;

        public TwitchChatService(TwitchApiService apiService, AuthenticationService authService, EmotesService emotesService)
        {
            _authService = authService;
            _emotesService = emotesService;

            _joinedChannels = new SourceList<string>();
            _client = new TwitchClient();

            apiService.UserConnected
                      .Subscribe(Connect);

            ChannelsJoined = _joinedChannels.Connect()
                                            .AsObservableList();

            ChatMessageReceived = Observable.FromEventPattern<OnMessageReceivedArgs>(_client, nameof(_client.OnMessageReceived))
                                            .Select(pattern => pattern.EventArgs.ChatMessage);
        }

        public Task Initialize() => Task.CompletedTask;

        public Task JoinChannel(string channel)
        {
            _client.JoinChannel(channel);
            _joinedChannels.Add(channel);

            return _emotesService.LoadChannelEmotes(channel);
        }

        public void LeaveChannel(string channel)
        {
            _client.LeaveChannel(channel);
            _joinedChannels.Remove(channel);

            _emotesService.UnloadChannelEmotes(channel);
        }


        private void Connect(User user)
        {
            _client.Initialize(new ConnectionCredentials(user.Login, _authService.TwitchAccessToken));
            _client.Connect();
        }
    }
}