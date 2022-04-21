﻿using ReactiveUI;

namespace ChatModApp.Shared.ViewModels;

public class MainViewModel : ReactiveObject, IScreen
{
    public RoutingState Router { get; }

    public MainViewModel(AuthenticationViewModel authenticationViewModel)
    {
        Router = new();
        authenticationViewModel.HostScreen = this;

        Router.NavigateAndReset.Execute(authenticationViewModel).Subscribe();
    }
}