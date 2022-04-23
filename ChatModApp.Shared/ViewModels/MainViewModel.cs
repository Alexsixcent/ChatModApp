using System.Reactive.Disposables;
using ChatModApp.Shared.Services;
using ReactiveUI;
using Splat;

namespace ChatModApp.Shared.ViewModels;

public class MainViewModel : ReactiveObject, IScreen, IActivatableViewModel
{
    public RoutingState Router { get; }
    
    private readonly AuthenticationService _authService;

    public MainViewModel(AuthenticationService authService)
    {
        _authService = authService;
        Router = new();
        Activator = new();

        this.WhenActivated(async disposable =>
        {
            if (await authService.TryAuthFromStorage())
            {
                var tab = Locator.Current.GetService<ChatTabViewModel>()!;
                tab.HostScreen = this;
                Router.Navigate
                      .Execute(tab)
                      .Subscribe()
                      .DisposeWith(disposable);
            }
            else
            {
                var auth = Locator.Current.GetService<AuthenticationViewModel>()!;
                auth.HostScreen = this;
                Router.Navigate
                      .Execute(auth)
                      .Subscribe()
                      .DisposeWith(disposable);
            }
        });
    }

    public ViewModelActivator Activator { get; }
}