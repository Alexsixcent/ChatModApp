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
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;

namespace ChatModApp.Shared.ViewModels;

public class ChatViewModel : ReactiveObject, IRoutableViewModel, IDisposable
{
    public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen? HostScreen { get; set; }

    public ITwitchChannel? Channel
    {
        get => _channel;
        set
        {
            _channel = value;
            if (_channel is not null)
            {
                Observable.FromAsync(token => _chatService.LoadMessageHistory(Channel!, token), RxApp.TaskpoolScheduler)
                          .Subscribe()
                          .DisposeWith(_disposables);
            }
        }
    }

    public ReactiveCommand<string, Unit> SendMessageCommand { get; }
    public ReactiveCommand<Unit, Unit> ChattersLoadCommand { get; }

    [Reactive] public string MessageText { get; set; }
    [Reactive] public string UserSearchText { get; set; }

    public ViewModelActivator Activator { get; }
    public ReadOnlyObservableCollection<ChatMessageViewModel> ChatMessages => _chatMessages;
    public ReadOnlyObservableCollection<IGrouping<UserType, string>> UsersList => _usersList;


    private readonly CompositeDisposable _disposables;
    private readonly TwitchChatService _chatService;
    private readonly SourceList<ChatterFormatted> _chatters;
    private readonly ReadOnlyObservableCollection<ChatMessageViewModel> _chatMessages;
    private readonly ReadOnlyObservableCollection<IGrouping<UserType, string>> _usersList;

    private ITwitchChannel? _channel;


    public ChatViewModel(TwitchChatService chatService, MessageProcessingService messageProcessingService)
    {
        MessageText = string.Empty;
        UserSearchText = string.Empty;
        Activator = new();
        _disposables = new();
        _chatters = new();
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
                    .Bind(out _chatMessages)
                    .Subscribe()
                    .DisposeWith(_disposables);

        _chatters.Connect()
                 .AutoRefreshOnObservable(_ => this.WhenAnyValue(vm => vm.UserSearchText))
                 .Filter(c => string.IsNullOrWhiteSpace(UserSearchText) || c.Username.Contains(UserSearchText))
                 .GroupByElement(c => c.UserType, c => c.Username)
                 .ObserveOn(RxApp.MainThreadScheduler)
                 .Bind(out _usersList)
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