using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Services;
using DynamicData;
using ReactiveUI;


namespace ChatModApp.ViewModels
{
    public class ChatViewModel : ReactiveObject, IRoutableViewModel, IDisposable
    {
        public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
        public IScreen? HostScreen { get; set; }

        public string? Channel { get; set; }
        public readonly ReadOnlyObservableCollection<ChatMessageViewModel> ChatMessages;


        private readonly CompositeDisposable _disposables;

        private readonly TwitchChatService _chatService;

        public ChatViewModel(TwitchChatService chatService, MessageProcessingService messageProcessingService)
        {
            _disposables = new CompositeDisposable();
            _chatService = chatService;

            _chatService.ChatMessageReceived
                        .Where(message => message.Channel == Channel)
                        .Select(messageProcessingService.ProcessMessageViewModel)
                        .ToObservableChangeSet(model => model.Id)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Bind(out ChatMessages)
                        .Subscribe()
                        .DisposeWith(_disposables);
        }

        public void Initialize()
        {
            if (string.IsNullOrWhiteSpace(Channel))
                throw new ArgumentNullException(nameof(Channel));

            _chatService.JoinChannel(Channel);
        }

        public void Dispose()
        {
            _disposables.Dispose();

            if (Channel is not null)
            {
                _chatService.LeaveChannel(Channel);
            }
        }
    }
}