using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.Shared.ViewModels;

public sealed class EmotePickerViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    public ViewModelActivator Activator { get; }

    [ObservableAsProperty] public ITwitchChannel? SrcChannel { get; }
    
    [Reactive] public string? SearchText { get; set; }
    [Reactive] public string? ChatViewMessageText { get; set; }
    
    public ReactiveCommand<IEmote, Unit> EmoteSubmittedCommand { get; }

    public ReadOnlyObservableCollection<IEmote>? FavoriteEmotes => _favoriteEmotes;
    public ReadOnlyObservableCollection<IGrouping<string, IMemberEmote>>? ChannelEmotes => _channelEmotes;
    public ReadOnlyObservableCollection<IGrouping<string, IGlobalEmote>>? GlobalEmotes => _globalEmotes;
    public ReadOnlyObservableCollection<IGrouping<string, EmojiEmote>>? Emojis => _emojis;


    private Func<IMemberEmote, bool> MemberEmoteFilter => emote => emote.MemberChannel == SrcChannel;

    private ReadOnlyObservableCollection<IEmote>? _favoriteEmotes;
    private ReadOnlyObservableCollection<IGrouping<string, IMemberEmote>>? _channelEmotes;
    private ReadOnlyObservableCollection<IGrouping<string, IGlobalEmote>>? _globalEmotes;
    private ReadOnlyObservableCollection<IGrouping<string, EmojiEmote>>? _emojis;

    public EmotePickerViewModel(EmotesService emotesService)
    {
        Activator = new();
        SrcChannel = null;
        EmoteSubmittedCommand = ReactiveCommand.Create<IEmote>(emote =>
        {
            if (emote is EmojiEmote emoji)
                ChatViewMessageText += emoji.EmojiValue + ' ';
            else
                ChatViewMessageText += emote.Code + ' ';
        });
        var favorites = new SourceList<IEmote>();

        this.WhenActivated(d =>
        {
            var channelFilter = this.WhenValueChanged(vm => vm.SrcChannel)
                                    .ObserveOnThreadPool()
                                    .DistinctUntilChanged()
                                    .Select(_ => MemberEmoteFilter);

            var emoteSearchFilter = this.WhenValueChanged(vm => vm.SearchText)
                                        .ObserveOnThreadPool()
                                        .DistinctUntilChanged()
                                        .Select<string?, Func<IEmote, bool>>(s => 
                                                                                 emote => string.IsNullOrWhiteSpace(s) || emote.Code.Contains(s, StringComparison.InvariantCultureIgnoreCase));
            
            favorites.Connect()
                     .Filter(emoteSearchFilter)
                     .ObserveOnMainThread()
                     .Bind(out _favoriteEmotes)
                     .Subscribe()
                     .DisposeWith(d);
            
            emotesService.Emotes
                         .Connect()
                         .WhereIsType<IEmote, IMemberEmote>()
                         .Filter(channelFilter)
                         .Filter(emoteSearchFilter)
                         .GroupByElement(emote => emote.Provider, emote => emote)
                         .ObserveOnMainThread()
                         .Bind(out _channelEmotes)
                         .Subscribe()
                         .DisposeWith(d);

            emotesService.Emotes
                         .Connect()
                         .WhereIsType<IEmote, IGlobalEmote>()
                         .Filter(emote => emote is not EmojiEmote)
                         .Filter(emoteSearchFilter)
                         .GroupByElement(emote => emote.Provider, emote => emote)
                         .ObserveOnMainThread()
                         .Bind(out _globalEmotes)
                         .Subscribe()
                         .DisposeWith(d);

            emotesService.Emotes
                         .Connect()
                         .WhereIsType<IEmote, EmojiEmote>()
                         .Filter(emoteSearchFilter)
                         .GroupByElement(emote => emote.EmojiGroup, emote => emote)
                         .ObserveOnMainThread()
                         .Bind(out _emojis)
                         .Subscribe()
                         .DisposeWith(d);
        });
    }

    public void Dispose()
    {
        Activator.Dispose();
    }
}