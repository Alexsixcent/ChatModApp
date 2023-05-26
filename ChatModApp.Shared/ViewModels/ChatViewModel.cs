using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.Shared.ViewModels;

public class ChatViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel, IDisposable
{
    public string UrlPathSegment => Guid.NewGuid().ToString()[..5];
    public IScreen? HostScreen { get; set; }

    public ViewModelActivator Activator { get; }
    public ReactiveCommand<string, Unit> SendMessageCommand { get; }

    [Reactive] public ITwitchChannel? Channel { get; set; }

    [Reactive] public string MessageText { get; set; }
    public EmotePickerViewModel EmotePicker { get; }
    public UserListViewModel UserList { get; }

    public ReadOnlyObservableCollection<ChatMessageViewModel> ChatMessages => _chatMessages;
    
    
    private readonly CompositeDisposable _disposables;
    private readonly TwitchChatService _chatService;
    
    private readonly ReadOnlyObservableCollection<ChatMessageViewModel> _chatMessages;
    
    public ChatViewModel(TwitchChatService chatService, MessageProcessingService msgProcService,
                         EmotePickerViewModel pickerViewModel, UserListViewModel userList)
    {
        MessageText = string.Empty;
        _disposables = new();
        _chatService = chatService;
        Activator = new();
        EmotePicker = pickerViewModel;
        UserList = userList;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.Channel)
                .ToPropertyEx(pickerViewModel, p => p.SrcChannel)
                .DisposeWith(d);

            this.WhenAnyValue(vm => vm.Channel)
                .ToPropertyEx(userList, p => p.SrcChannel)
                .DisposeWith(d);
            
            this.WhenAnyValue(vm => vm.MessageText)
                .BindTo(pickerViewModel, vm => vm.ChatViewMessageText)
                .DisposeWith(d);
            pickerViewModel.WhenAnyValue(vm => vm.ChatViewMessageText)
                           .BindTo(this, vm => vm.MessageText)
                           .DisposeWith(d);
        });



        var messageSent = _chatService.ChatMessageSent
                                      .Where(message => message.Channel == Channel?.Login)
                                      .Select(msg => msgProcService.ProcessSentMessage(Channel!, msg));
        
        _chatService.ChatMessageReceived
                    .Where(message => message.Channel == Channel?.Login)
                    .Select(msg => msgProcService.ProcessReceivedMessage(Channel!, msg))
                    .Merge(messageSent)
                    .ToObservableChangeSet(model => model.Id)
                    .ObserveOnMainThread()
                    .Bind(out _chatMessages)
                    .Subscribe()
                    .DisposeWith(_disposables);




        SendMessageCommand = ReactiveCommand.Create<string>(message =>
        {
            _chatService.SendMessage(Channel ?? throw new NullReferenceException("Channel can't be null"), message);
            MessageText = string.Empty;
        }).DisposeWith(_disposables);


    }


    public void Dispose() => _disposables.Dispose();
}