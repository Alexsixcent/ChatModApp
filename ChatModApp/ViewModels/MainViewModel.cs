using System;
using System.Reactive;
using ReactiveUI;

namespace ChatModApp.ViewModels
{
    public class MainViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; }

        public readonly ChatViewModel ChatViewModel;

        public MainViewModel(ChatViewModel chatViewModel, AuthenticationViewModel authenticationViewModel)
        {
            ChatViewModel = chatViewModel;

            Router = new RoutingState();
            authenticationViewModel.HostScreen = this;

            Router.NavigateAndReset.Execute(authenticationViewModel).Subscribe();
        }
    }
}