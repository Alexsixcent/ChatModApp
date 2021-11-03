using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Models;
using ChatModApp.Services;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.ViewModels;

public class ChatViewModel : ReactiveObject, IRoutableViewModel, IDisposable
{
    public readonly ReadOnlyObservableCollection<ChatMessageViewModel> ChatMessages;
    public ITwitchChannel? Channel { get; set; }
    public ReactiveCommand<string, Unit> SendMessageCommand { get; }

    [Reactive]
    public string MessageText { get; set; }

    public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen? HostScreen { get; set; }

    private readonly CompositeDisposable _disposables;

    public ChatViewModel(TwitchChatService chatService, MessageProcessingService messageProcessingService)
    {
        _disposables = new();
        var chatService1 = chatService;

        var messageSent = chatService1.ChatMessageSent.Where(message => message.Channel == Channel?.Login)
                                      .Select(messageProcessingService.ProcessSentMessage);

        chatService1.ChatMessageReceived.Where(message => message.Channel == Channel?.Login)
                    .Select(messageProcessingService.ProcessReceivedMessage).Merge(messageSent)
                    .ToObservableChangeSet(model => model.Id).ObserveOn(RxApp.MainThreadScheduler).Bind(out ChatMessages)
                    .Subscribe().DisposeWith(_disposables);

        MessageText = string.Empty;
        SendMessageCommand = ReactiveCommand.Create<string>(s =>
        {
            chatService1.SendMessage(Channel!, s);
            MessageText = string.Empty;
        });

        SendMessageCommand.DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}