using System;
using System.Reactive;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ChatModApp.ViewModels
{
    public class ChatTabViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
        public IScreen? HostScreen { get; set; }

        [Reactive] public string ChannelNameSubmit { get; set; }

        public ReactiveCommand<string, Unit> AddTabCommand { get; }
        public ReactiveCommand<ChatTabItemViewModel, Unit> CloseTabCommand { get; }
        public ObservableCollectionExtended<ChatTabItemViewModel> ChatTabs { get; }

        public ChatTabViewModel()
        {
            ChannelNameSubmit = "";
            ChatTabs = new ObservableCollectionExtended<ChatTabItemViewModel>();

            AddTabCommand = ReactiveCommand.Create<string>(s => ChatTabs.Add(CreateTab(s)));
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