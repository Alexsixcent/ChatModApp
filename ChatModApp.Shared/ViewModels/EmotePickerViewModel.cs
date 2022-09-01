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

public class EmotePickerViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }

    [ObservableAsProperty] public ITwitchChannel? SrcChannel { get; }

    public ReadOnlyObservableCollection<IGrouping<string, IMemberEmote>> ChannelEmotes => _channelEmotes;


    private Func<IMemberEmote, bool> MemberEmoteFilter => emote =>
        emote.MemberChannel.Equals(SrcChannel?.Login, StringComparison.InvariantCultureIgnoreCase);

    private ReadOnlyObservableCollection<IGrouping<string, IMemberEmote>> _channelEmotes = null!;


    public EmotePickerViewModel(EmotesService emotesService)
    {
        Activator = new();
        SrcChannel = null;

        var filterChanged = this.WhenValueChanged(vm => vm.SrcChannel)
                                .ObserveOnThreadPool()
                                .DistinctUntilChanged()
                                .Select(_ => MemberEmoteFilter);

        this.WhenActivated(disposable =>
        {
            emotesService.Emotes
                         .Connect()
                         .ObserveOnThreadPool()
                         .WhereIsType<IEmote, IMemberEmote>()
                         .Filter(filterChanged)
                         .GroupByElement(emote => emote.Provider, emote => emote)
                         .ObserveOnMainThread()
                         .Bind(out _channelEmotes)
                         .Subscribe()
                         .DisposeWith(disposable);
        });
    }
}