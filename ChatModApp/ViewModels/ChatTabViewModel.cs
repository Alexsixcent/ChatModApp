﻿using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ChatModApp.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ChatModApp.ViewModels
{
    public class ChatTabViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment => "chatTabs";
        public IScreen? HostScreen { get; set; }

        public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
        public ReactiveCommand<ChatTabItemViewModel, Unit> CloseTabCommand { get; }
        
        [Reactive]
        public int OpenedTabIndex { get; private set; }

        public readonly ObservableCollectionExtended<ChatTabItemViewModel> ChatTabs;


        private readonly ChatTabService _tabService;

        public ChatTabViewModel(TwitchChatService chatService, ChatTabService tabService)
        {
            _tabService = tabService;
            ChatTabs = new ObservableCollectionExtended<ChatTabItemViewModel>();

            AddTabCommand = ReactiveCommand.Create(AddTab);
            CloseTabCommand = ReactiveCommand.Create<ChatTabItemViewModel>(RemoveTab);

            _tabService.Tabs
                       .Cast(item => (ChatTabItemViewModel)item)
                       .ObserveOn(RxApp.MainThreadScheduler)
                       .Bind(ChatTabs)
                       .Subscribe();
        }

        private void AddTab()
        {
            var newTab = Locator.Current.GetService<ChatTabItemViewModel>();
            newTab.Title = "New tab";

            _tabService.AddTab(newTab);
            OpenedTabIndex = ChatTabs.Count - 1;
        }

        private void RemoveTab(ChatTabItemViewModel tab)
        {
            _tabService.RemoveTab(tab.Id);
            tab.Dispose();
        }
    }
}