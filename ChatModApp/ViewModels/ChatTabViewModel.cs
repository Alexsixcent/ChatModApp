using System;
using System.Reactive;
using DynamicData.Binding;
using ReactiveUI;
using Splat;

namespace ChatModApp.ViewModels
{
    public class ChatTabViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
        public IScreen? HostScreen { get; set; }

        public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
        public ReactiveCommand<ChatTabItemViewModel, Unit> CloseTabCommand { get; }
        public ObservableCollectionExtended<ChatTabItemViewModel> ChatTabs { get; }

        public ChatTabViewModel()
        {
            ChatTabs = new ObservableCollectionExtended<ChatTabItemViewModel>();

            AddTabCommand = ReactiveCommand.Create(() => ChatTabs.Add(CreateTab()));
            CloseTabCommand = ReactiveCommand.Create<ChatTabItemViewModel>(tab => ChatTabs.Remove(tab));
        }

        private ChatTabItemViewModel CreateTab(string? title = null)
        {
            var newTab = Locator.Current.GetService<ChatTabItemViewModel>();
            
            newTab.Title = title ?? $"New tab {ChatTabs.Count + 1}";

            return newTab;
        }
    }
}