using System.Reactive.Disposables;
using ReactiveUI;

namespace ChatModApp.Shared.ViewModels;

public class MainViewModel : ReactiveObject, IScreen, IActivatableViewModel
{
    public RoutingState Router { get; }

    public MainViewModel(AuthenticationViewModel authenticationViewModel)
    {
        Router = new();
        Activator = new();
        authenticationViewModel.HostScreen = this;

        this.WhenActivated(disposable =>
        {
            Router.Navigate
                  .Execute(authenticationViewModel)
                  .Subscribe()
                  .DisposeWith(disposable);
        });
    }

    public ViewModelActivator Activator { get; }
}