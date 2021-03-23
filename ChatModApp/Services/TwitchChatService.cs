using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace ChatModApp.Services
{
    public class TwitchChatService : BackgroundService
    {
        public IObservable<ChatMessage> ChatMessageReceived { get; }

        private readonly ILogger<TwitchChatService> _logger;
        private readonly AuthenticationService _authService;
        private readonly TwitchClient _client;
        private readonly TwitchAPI _api;

        public TwitchChatService(ILogger<TwitchChatService> logger, AuthenticationService authService)
        {
            _logger = logger;
            _authService = authService;

            _api = new TwitchAPI();
            _client = new TwitchClient();

            _authService.AccessTokenChanged
                .Select(s => Connect(s).ToObservable())
                .Concat()
                .Subscribe();


            ChatMessageReceived = Observable.FromEventPattern<OnMessageReceivedArgs>(
                    handler => _client.OnMessageReceived += handler,
                    handler => _client.OnMessageReceived -= handler)
                .Select(pattern => pattern.EventArgs.ChatMessage);
        }

        public async Task Connect(string accessToken)
        {
            _logger.LogInformation("CONNECT");
            _api.Settings.ClientId = AuthenticationService.ClientId;
            _api.Settings.AccessToken = accessToken;

            var res = await _api.Helix.Users.GetUsersAsync();
            var currentUser = res.Users.Single();

            _client.Initialize(new ConnectionCredentials(currentUser.Login, _authService.TwitchAccessToken));
            _client.Connect();
        }

        public void JoinChannel(string channel) => _client.JoinChannel(channel);
        public void LeaveChannel(string channel) => _client.LeaveChannel(channel);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TASK EXEC");
            if (_authService.IsAuthenticated)
            {
                await Connect(_authService.TwitchAccessToken);
            }
        }
    }
}