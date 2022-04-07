﻿using System;
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
    public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen? HostScreen { get; set; }

    public ITwitchChannel? Channel { get; set; }

    public ReactiveCommand<string, Unit> SendMessageCommand { get; }

    [Reactive]
    public string MessageText { get; set; }

    public readonly ReadOnlyObservableCollection<ChatMessageViewModel> ChatMessages;


    private readonly CompositeDisposable _disposables;
    private readonly TwitchChatService _chatService;

    public ChatViewModel(TwitchChatService chatService, MessageProcessingService messageProcessingService)
    {
        _disposables = new();
        _chatService = chatService;

        var messageSent= _chatService.ChatMessageSent
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

        MessageText = string.Empty;
        SendMessageCommand = ReactiveCommand.Create<string>(s =>
        {
            _chatService.SendMessage(Channel!, s);
            MessageText = string.Empty;
        });

        SendMessageCommand.DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}