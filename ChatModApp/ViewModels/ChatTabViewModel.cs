using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Splat;

namespace ChatModApp.ViewModels
{
    public class ChatTabViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
        public IScreen HostScreen { get; set; }


        public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
        public readonly ReadOnlyObservableCollection<ChatTabItemViewModel> ChatTabs;


        private readonly ObservableCollectionExtended<ChatTabItemViewModel> _chatTabs;

        public ChatTabViewModel()
        {
            AddTabCommand = ReactiveCommand.Create(() =>
            {
                var newTab = Locator.Current.GetService<ChatTabItemViewModel>();
                newTab.Title = $"new tab {_chatTabs.Count}";
                _chatTabs.Add(newTab);
            });


            _chatTabs = new ObservableCollectionExtended<ChatTabItemViewModel>();
            _chatTabs
                .ToObservableChangeSet()
                .Bind(out ChatTabs)
                .Subscribe();

            var newTab = Locator.Current.GetService<ChatTabItemViewModel>();
            newTab.Title = "New Tab 1";

            _chatTabs.Add(newTab);
        }
    }
}