using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using ChatModApp.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;


namespace ChatModApp.ViewModels
{
    public class ChatViewModel : ReactiveObject
    {
        public readonly ICommand SubmitCommand;
        public readonly ReadOnlyObservableCollection<ChatMessageViewModel> ChatMessages;


        private readonly TwitchChatService _chatService;

        public ChatViewModel(TwitchChatService chatService)
        {
            _chatService = chatService;


            SubmitCommand = ReactiveCommand.Create(_chatService.Test);
            _chatService.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out ChatMessages)
                .Subscribe();
        }
    }
}