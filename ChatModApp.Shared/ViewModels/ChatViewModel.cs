using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Models;
using ChatModApp.Services;
using ChatModApp.Tools.Extensions;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;

namespace ChatModApp.ViewModels;

public class ChatViewModel : ReactiveObject, IRoutableViewModel, IDisposable
{
    public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen? HostScreen { get; set; }

    public ITwitchChannel? Channel { get; set; }

    public ReactiveCommand<string, Unit> SendMessageCommand { get; }
    public ReactiveCommand<Unit, Unit> ChattersLoadCommand { get; }

    [Reactive] public string MessageText { get; set; }
    [Reactive] public string UserSearchText { get; set; }

    public readonly ReadOnlyObservableCollection<ChatMessageViewModel> ChatMessages;
    public readonly ReadOnlyObservableCollection<IGrouping<UserType, string>> UsersList;


    private readonly CompositeDisposable _disposables;
    private readonly TwitchChatService _chatService;
    private readonly SourceList<ChatterFormatted> _chatters;


    public ChatViewModel(TwitchChatService chatService, MessageProcessingService messageProcessingService)
    {
        _disposables = new();
        _chatters = new();
        MessageText = string.Empty;
        UserSearchText = string.Empty;
        _chatService = chatService;

        var messageSent = _chatService.ChatMessageSent
                                      .Where(message => message.Channel == Channel?.Login)
                                      .Select(messageProcessingService.ProcessSentMessage);

        _chatService.ChatMessageReceived
                    .Where(message => message.Channel == Channel?.Login)
                    .Select(messageProcessingService.ProcessReceivedMessage)
                    .Merge(messageSent)
                    .ToObservableChangeSet(model => model.Id)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Bind(out ChatMessages)
                    .Subscribe()
                    .DisposeWith(_disposables);
        
        _chatters.Connect()
                 .AutoRefreshOnObservable(_ => this.WhenAnyValue(vm => vm.UserSearchText))
                 .Filter(c => c.Username.Contains(UserSearchText))
                 .GroupByElement(c => c.UserType, c => c.Username)
                 .ObserveOn(RxApp.MainThreadScheduler)
                 .Bind(out UsersList)
                 .Subscribe()
                 .DisposeWith(_disposables);

        SendMessageCommand = ReactiveCommand.Create<string>(message =>
        {
            _chatService.SendMessage(Channel ?? throw new NullReferenceException("Channel can't be null"), message);
            MessageText = string.Empty;
        }).DisposeWith(_disposables);

        ChattersLoadCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var list =
                await _chatService.GetChatUserList(Channel ??
                                                   throw new NullReferenceException("Channel can't be null"))
                                  .ConfigureAwait(false);

            _chatters.Edit(e =>
            {
                e.Clear();
                e.AddRange(list);
            });
        }).DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}