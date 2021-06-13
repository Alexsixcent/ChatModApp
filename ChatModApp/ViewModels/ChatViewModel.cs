using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Services;
using DynamicData;
using ReactiveUI;
using Tools.Extensions;


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

        public ChatViewModel(TwitchChatService chatService, EmotesService emotesService)
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
                        .Subscribe(message =>
                        {
                            _chatMessages.Add(new ChatMessageViewModel
                            {
                                Username = message.DisplayName,
                                Message = emotesService.GetMessageFragments(message),
                                UsernameColor = string.IsNullOrEmpty(message.ColorHex)
                                    ? Color.Gray
                                    : Color.FromArgb(int.Parse(message.ColorHex.TrimStart(1), NumberStyles.HexNumber))
                            });
                        })
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