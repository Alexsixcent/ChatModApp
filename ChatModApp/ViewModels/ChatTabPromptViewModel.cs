using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatModApp.Services;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.ViewModels
{
    public class ChatTabPromptViewModel : ReactiveObject, IDisposable, IRoutableViewModel
    {
        public string UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
        public IScreen HostScreen { get; set; }
        public Guid ParentTabId { get; set; }

        public ReadOnlyObservableCollection<ChannelSuggestionViewModel> ChannelSuggestions;
        public ReactiveCommand<ChannelSuggestionViewModel, Unit> SelectionCommand { get; }

        [Reactive]
        public string Channel { get; set; }


        private readonly CompositeDisposable _disposables;
        private readonly ChatViewModel _chatViewModel;
        private readonly ChatTabService _tabService;
        private readonly TwitchApiService _apiService;

        public ChatTabPromptViewModel(ChatViewModel chatViewModel, ChatTabService tabService,
                                      TwitchApiService apiService)
        {
            _disposables = new CompositeDisposable();
            _chatViewModel = chatViewModel;
            _tabService = tabService;
            _apiService = apiService;
            Channel = string.Empty;

            this.WhenAnyValue(vm => vm.Channel)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Throttle(TimeSpan.FromSeconds(0.3), RxApp.TaskpoolScheduler)
                .SelectMany(SearchChannels)
                .ToObservableChangeSet(20)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out ChannelSuggestions)
                .Subscribe()
                .DisposeWith(_disposables);

            SelectionCommand = ReactiveCommand.Create<ChannelSuggestionViewModel>(OpenChannel);

            SelectionCommand.DisposeWith(_disposables);
            _chatViewModel.DisposeWith(_disposables);
        }

        public void Dispose() => _disposables.Dispose();


        private async Task<IEnumerable<ChannelSuggestionViewModel>> SearchChannels(string searchTerm, CancellationToken cancel)
        {
            var res = await _apiService.Helix.Search.SearchChannelsAsync(searchTerm);

            return res.Channels.Select(ch => new ChannelSuggestionViewModel(ch.BroadcasterLogin, ch.DisplayName, new Uri(ch.ThumbnailUrl), ch.IsLive));
        }

        private void OpenChannel(ChannelSuggestionViewModel channel)
        {
            var tab = _tabService.TabCache.Lookup(ParentTabId).Value;
            tab.Title = tab.Channel = channel.DisplayName;

            _chatViewModel.Channel = channel.Login;
            _chatViewModel.HostScreen = HostScreen;
            HostScreen.Router.Navigate.Execute(_chatViewModel)
                      .Subscribe()
                      .DisposeWith(_disposables);

            _chatViewModel.Initialize();
        }
    }
}