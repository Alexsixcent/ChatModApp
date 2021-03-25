using System;
using ReactiveUI;

namespace ChatModApp.ViewModels
{
    public class MainViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; }

        public MainViewModel(AuthenticationViewModel authenticationViewModel)
        {
            Router = new RoutingState();
            authenticationViewModel.HostScreen = this;

            Router.NavigateAndReset.Execute(authenticationViewModel).Subscribe();
        }
    }
}