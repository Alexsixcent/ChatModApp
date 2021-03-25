using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Tools;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace ChatModApp.Services
{
    public class TwitchChatService : IService
    {
        public IObservable<ChatMessage> ChatMessageReceived { get; }

        private readonly AuthenticationService _authService;
        private readonly TwitchClient _client;

        public TwitchChatService(TwitchApiService apiService, AuthenticationService authService)
        {
            _authService = authService;

            _client = new TwitchClient();

            apiService.Connected
                .Subscribe(Connect);

            ChatMessageReceived = Observable.FromEventPattern<OnMessageReceivedArgs>(
                    handler => _client.OnMessageReceived += handler,
                    handler => _client.OnMessageReceived -= handler)
                .Select(pattern => pattern.EventArgs.ChatMessage);
        }

        public Task Initialize() => Task.CompletedTask;

        public void JoinChannel(string channel) => _client.JoinChannel(channel);
        public void LeaveChannel(string channel) => _client.LeaveChannel(channel);


        private void Connect(User user)
        {
            _client.Initialize(new ConnectionCredentials(user.Login, _authService.TwitchAccessToken));
            _client.Connect();
        }
    }
}