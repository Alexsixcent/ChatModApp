using System;
using ChatModApp.ViewModels;
using DynamicData;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace ChatModApp.Services
{
    public class TwitchChatService
    {
        public IObservable<IChangeSet<ChatMessageViewModel>> Connect() => _chatMessages.Connect();


        private readonly SourceList<ChatMessageViewModel> _chatMessages;

        private readonly ILogger<TwitchChatService> _logger;
        private readonly TwitchClient _client;

        public TwitchChatService(ILogger<TwitchChatService> logger)
        {
            _logger = logger;
            var credentials = new ConnectionCredentials("Alexsixcent", "access_token");
            _client = new TwitchClient();
            //_client.Initialize(credentials);

            _chatMessages = new SourceList<ChatMessageViewModel>();
            _chatMessages.AddRange(new[]
            {
                new ChatMessageViewModel {Message = "Test", Username = "This is a test."},
                new ChatMessageViewModel {Message = "Test2", Username = "This is a test."},
                new ChatMessageViewModel {Message = "Test3", Username = "This is a test."}
            });
        }

        public void Test()
        {
            _logger.LogInformation("LOGGER TEST");
        }
    }
}