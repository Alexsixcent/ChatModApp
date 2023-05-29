using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;
using DynamicData.Alias;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.Shared.ViewModels;

public sealed class ChatViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel
{
    public string UrlPathSegment => Guid.NewGuid().ToString()[..5];
    public IScreen? HostScreen { get; set; }

    public ViewModelActivator Activator { get; }
    public ReactiveCommand<string, Unit> SendMessageCommand { get; private set; } = null!;

    [Reactive] public ITwitchUser? Channel { get; set; }
    [Reactive] public string MessageText { get; set; }
    public EmotePickerViewModel EmotePicker { get; }
    public UserListViewModel UserList { get; }
    [Reactive] public ReadOnlyObservableCollection<IChatMessage>? ChatMessages { get; private set; }

    public ChatViewModel(TwitchChatService chatService, MessageProcessingService msgProcService,
                         EmotePickerViewModel pickerViewModel, UserListViewModel userList)
    {
        MessageText = string.Empty;
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

            msgProcService.ChannelMessages
                          .Where(msg => msg.Channel == Channel)
                          .ObserveOnMainThread()
                          .Bind(out var messages)
                          .Subscribe()
                          .DisposeWith(d);
            ChatMessages = messages;
            
            SendMessageCommand = ReactiveCommand.Create<string>(message =>
            {
                chatService.SendMessage(Channel ?? throw new NullReferenceException("Channel can't be null"), message);
                MessageText = string.Empty;
            }).DisposeWith(d);
        });
    }
}