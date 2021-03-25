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


        private readonly SourceList<ChatMessageViewModel> _chatMessages;
        private readonly CompositeDisposable _disposables;

        private readonly TwitchChatService _chatService;

        public ChatViewModel(TwitchChatService chatService)
        {
            _chatMessages = new SourceList<ChatMessageViewModel>();
            _disposables = new CompositeDisposable();
            _chatService = chatService;

            _chatMessages.DisposeWith(_disposables);

            _chatMessages.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out ChatMessages)
                .Subscribe()
                .DisposeWith(_disposables);

            _chatService.ChatMessageReceived
                .Where(message => message.Channel == Channel)
                .Subscribe(message => _chatMessages.Add(new ChatMessageViewModel
                {
                    Username = message.Username, 
                    Message = message.Message,
                    UsernameColor = message.Color
                }))
                .DisposeWith(_disposables);
        }

        public void Initialize() => _chatService.JoinChannel(Channel);

        public void Dispose()
        {
            _disposables.Dispose();

            if (Channel is not null) 
                _chatService.LeaveChannel(Channel);
        }
    }
}