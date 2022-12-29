using System.Collections.ObjectModel;
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

    public ReadOnlyObservableCollection<IEmote> FavoriteEmotes => _favoriteEmotes;
    public ReadOnlyObservableCollection<IGrouping<string, IMemberEmote>> ChannelEmotes => _channelEmotes;
    public ReadOnlyObservableCollection<IGrouping<string, IGlobalEmote>> GlobalEmotes => _globalEmotes;
    public ReadOnlyObservableCollection<IGrouping<string, EmojiEmote>> Emojis => _emojis;


    private Func<IMemberEmote, bool> MemberEmoteFilter => emote => emote.MemberChannel == SrcChannel;

    private readonly CompositeDisposable _disposables;

    private readonly ReadOnlyObservableCollection<IEmote> _favoriteEmotes;
    private readonly ReadOnlyObservableCollection<IGrouping<string, IMemberEmote>> _channelEmotes;
    private readonly ReadOnlyObservableCollection<IGrouping<string, IGlobalEmote>> _globalEmotes;
    private readonly ReadOnlyObservableCollection<IGrouping<string, EmojiEmote>> _emojis;

    public EmotePickerViewModel(EmotesService emotesService)
    {
        _disposables = new();
        Activator = new();
        SrcChannel = null;

        var favorites = new SourceList<IEmote>();
        favorites.Connect()
                 .ObserveOnMainThread()
                 .Bind(out _favoriteEmotes)
                 .Subscribe()
                 .DisposeWith(_disposables);
        
        var filterChanged = this.WhenValueChanged(vm => vm.SrcChannel)
                                .ObserveOnThreadPool()
                                .DistinctUntilChanged()
                                .Select(_ => MemberEmoteFilter);
        
        emotesService.Emotes
                     .Connect()
                     .WhereIsType<IEmote, IMemberEmote>()
                     .Filter(filterChanged)
                     .GroupByElement(emote => emote.Provider, emote => emote)
                     .ObserveOnMainThread()
                     .Bind(out _channelEmotes)
                     .Subscribe()
                     .DisposeWith(_disposables);

        emotesService.Emotes
                     .Connect()
                     .WhereIsType<IEmote, IGlobalEmote>()
                     .Filter(emote => emote is not EmojiEmote)
                     .GroupByElement(emote => emote.Provider, emote => emote)
                     .ObserveOnMainThread()
                     .Bind(out _globalEmotes)
                     .Subscribe()
                     .DisposeWith(_disposables);

        emotesService.Emotes
                     .Connect()
                     .WhereIsType<IEmote, EmojiEmote>()
                     .GroupByElement(emote => emote.EmojiGroup, emote => emote)
                     .ObserveOnMainThread()
                     .Bind(out _emojis)
                     .Subscribe()
                     .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        Activator.Dispose();
        _disposables.Dispose();
    }
}