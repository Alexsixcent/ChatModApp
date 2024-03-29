﻿using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Tools.Extensions;
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
            .WhereNotNullOrWhiteSpace()
            .QuickThrottle(TimeSpan.FromSeconds(0.5), RxApp.TaskpoolScheduler)
            .Log(this, "Selected channel changed")
            .ObserveOnThreadPool()
            .SelectMany(SearchChannels)
            .ToObservableChangeSet(20)
            .Sort(SortExpressionComparer<ChannelSuggestionViewModel>.Descending(model => model.IsLive))
            .ObserveOnMainThread()
            .Bind(out _channelSuggestions)
            .Subscribe()
            .DisposeWith(_disposables);

        SelectionCommand = ReactiveCommand.CreateFromTask<ChannelSuggestionViewModel>(OpenChannel);

        SelectionCommand.DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();


    private async Task<IEnumerable<ChannelSuggestionViewModel>> SearchChannels(
        string searchTerm, CancellationToken cancel)
    {
        var res = await _apiService.Helix.Search.SearchChannelsAsync(searchTerm);

        return res.Channels.Select(ch => new ChannelSuggestionViewModel(new TwitchUser(ch.Id, ch.BroadcasterLogin, ch.DisplayName),
                                                                        new(ch.ThumbnailUrl), ch.IsLive));
    }

    private async Task OpenChannel(ChannelSuggestionViewModel suggestion)
    {
        var channel = suggestion.Channel;
        var tab = _tabService.TabCache.Lookup(ParentTabId).Value;

        tab.Channel = channel;
        tab.Title = channel.DisplayName;
        tab.ChannelIcon = suggestion.ThumbnailUrl;
        
        _chatViewModel.Channel = channel;
        _chatViewModel.HostScreen = HostScreen;

        await (HostScreen?.Router.Navigate.Execute(_chatViewModel) ?? throw new InvalidOperationException());
    }
}