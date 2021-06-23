using System.Reactive;
using ChatModApp.Services;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ChatModApp.ViewModels
{
    public class ChatTabViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment { get; } = "chatTabs";
        public IScreen? HostScreen { get; set; }

        public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
        public ReactiveCommand<ChatTabItemViewModel, Unit> CloseTabCommand { get; }
        
        [Reactive]
        public int OpenedTabIndex { get; private set; }
        public ObservableCollectionExtended<ChatTabItemViewModel> ChatTabs { get; }

        public ChatTabViewModel(TwitchChatService chatService)
        {
            ChatTabs = new ObservableCollectionExtended<ChatTabItemViewModel>();

            AddTabCommand = ReactiveCommand.Create(AddTab);
            CloseTabCommand = ReactiveCommand.Create<ChatTabItemViewModel>(RemoveTab);
        }

        private void AddTab()
        {
            var newTab = Locator.Current.GetService<ChatTabItemViewModel>();
            newTab.Title = "New tab";

            ChatTabs.Add(newTab);
            OpenedTabIndex = ChatTabs.Count - 1;
        }

        private void RemoveTab(ChatTabItemViewModel tab)
        {
            ChatTabs.Remove(tab);
            tab.Dispose();
        }
    }
}