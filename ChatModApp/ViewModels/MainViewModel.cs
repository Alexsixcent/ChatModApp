using System;
using System.Reactive;
using ReactiveUI;

namespace ChatModApp.ViewModels
{
    public class MainViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; }

        public MainViewModel(ChatTabViewModel chatTabViewModel, AuthenticationViewModel authenticationViewModel)
        {
            Router = new RoutingState();
            authenticationViewModel.HostScreen = this;

            Router.NavigateAndReset.Execute(chatTabViewModel).Subscribe();
            //Router.NavigateAndReset.Execute(authenticationViewModel).Subscribe();
        }
    }
}