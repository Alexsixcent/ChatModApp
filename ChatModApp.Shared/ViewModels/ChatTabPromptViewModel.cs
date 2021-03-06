using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.Shared.ViewModels;

public class ChatTabPromptViewModel : ReactiveObject, IDisposable, IRoutableViewModel
{
    public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen? HostScreen { get; set; }
    public Guid ParentTabId { get; set; }

    public ReadOnlyObservableCollection<ChannelSuggestionViewModel> ChannelSuggestions => _channelSuggestions;

    public ReactiveCommand<ChannelSuggestionViewModel, Unit> SelectionCommand { get; }

    [Reactive] public string Channel { get; set; }


    private readonly CompositeDisposable _disposables;
    private readonly ChatViewModel _chatViewModel;
    private readonly ChatTabService _tabService;
    private readonly TwitchApiService _apiService;
    private readonly ReadOnlyObservableCollection<ChannelSuggestionViewModel> _channelSuggestions;

    public ChatTabPromptViewModel(ChatViewModel chatViewModel, ChatTabService tabService, TwitchApiService apiService)
    {
        _disposables = new();
        _chatViewModel = chatViewModel;
        _tabService = tabService;
        _apiService = apiService;
        Channel = string.Empty;

        this.WhenAnyValue(vm => vm.Channel)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(SearchChannels)
            .ToObservableChangeSet(20)
            .Sort(SortExpressionComparer<ChannelSuggestionViewModel>.Descending(model => model.IsLive))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _channelSuggestions)
            .Subscribe()
            .DisposeWith(_disposables);

        SelectionCommand = ReactiveCommand.Create<ChannelSuggestionViewModel>(OpenChannel);

        SelectionCommand.DisposeWith(_disposables);
        _chatViewModel.DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();


    private async Task<IEnumerable<ChannelSuggestionViewModel>> SearchChannels(
        string searchTerm, CancellationToken cancel)
    {
        var res = await _apiService.Helix.Search.SearchChannelsAsync(searchTerm);

        return res.Channels.Select(ch => new ChannelSuggestionViewModel(ch.BroadcasterLogin, ch.DisplayName,
                                                                        new(ch.ThumbnailUrl), ch.IsLive));
    }

    private void OpenChannel(ChannelSuggestionViewModel suggestion)
    {
        var channel = new TwitchChannel(suggestion.DisplayName, suggestion.Login);
        var tab = _tabService.TabCache.Lookup(ParentTabId).Value;

        tab.Channel = new TwitchChannel(channel.DisplayName, channel.Login);
        tab.Title = channel.DisplayName;
        _chatViewModel.Channel = channel;
        _chatViewModel.HostScreen = HostScreen;

        HostScreen?.Router.Navigate.Execute(_chatViewModel)
                  .Subscribe()
                  .DisposeWith(_disposables);
    }
}