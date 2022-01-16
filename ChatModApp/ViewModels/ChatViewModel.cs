using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    public readonly ReadOnlyObservableCollection<object> ChatSuggestions;
    public ITwitchChannel? Channel { get; set; }
    public ReactiveCommand<string, Unit> SendMessageCommand { get; }
    public ReactiveCommand<(string, string), Unit> SuggestionRequestCommand { get; }

    [Reactive] public string MessageText { get; set; }


    public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen? HostScreen { get; set; }


    private readonly CompositeDisposable _disposables;
    private readonly EmotesService _emotesService;

    private readonly SourceList<object> _suggestions;

    public ChatViewModel(TwitchChatService chatService, EmotesService emotesService,
                         MessageProcessingService messageProcessingService)
    {
        _emotesService = emotesService;
        _suggestions = new();
        _disposables = new();
        MessageText = string.Empty;

        var messageSent = chatService.ChatMessageSent.Where(message => message.Channel == Channel?.Login)
                                     .Select(messageProcessingService.ProcessSentMessage);

        chatService.ChatMessageReceived.Where(message => message.Channel == Channel?.Login)
                   .Select(messageProcessingService.ProcessReceivedMessage)
                   .Merge(messageSent)
                   .ToObservableChangeSet(model => model.Id).ObserveOn(RxApp.MainThreadScheduler)
                   .Bind(out ChatMessages)
                   .Subscribe().DisposeWith(_disposables);

        _suggestions.Connect().Bind(out ChatSuggestions).Subscribe().DisposeWith(_disposables);

        SendMessageCommand = ReactiveCommand.Create<string>(s =>
        {
            chatService.SendMessage(Channel!, s);
            MessageText = string.Empty;
        });
        SuggestionRequestCommand = ReactiveCommand.Create<(string, string)>(SuggestionRequest);

        SendMessageCommand.DisposeWith(_disposables);
        SuggestionRequestCommand.DisposeWith(_disposables);
    }

    private void SuggestionRequest((string Prefix, string Query) args)
    {
        if (args.Prefix == ":")
        {
            var emotes = _emotesService.GlobalEmotes.Items;
            
            var res = _emotesService.UserEmotes.Lookup(Channel!.Login);
            if (res.HasValue) 
                emotes = emotes.Concat(res.Value.Cache.Items);

            emotes = emotes.Where(emote => emote.Code.Contains(args.Query, StringComparison.OrdinalIgnoreCase))
                           .OrderBy(emote => emote.Code, StringComparer.OrdinalIgnoreCase);
            
            _suggestions.Edit(update =>
            {
                update.Clear();
                update.AddRange(emotes);
            });
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}