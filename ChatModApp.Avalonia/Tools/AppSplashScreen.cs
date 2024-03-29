using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.ViewModels;
using ChatModApp.Views;
using FluentAvalonia.UI.Windowing;
using Splat;

namespace ChatModApp.Tools;

public class AppSplashScreen : IApplicationSplashScreen
{
    private readonly GlobalStateService _stateService;
    private readonly AuthenticationService _authService;
    private readonly MainViewModel _mainViewModel;

    public AppSplashScreen(GlobalStateService stateService, AuthenticationService authService, MainViewModel mainViewModel)
    {
        _stateService = stateService;
        _authService = authService;
        _mainViewModel = mainViewModel;
        AppName = null;
        AppIcon = null;
        SplashScreenContent = new AppSplashScreenView();
    }

    public async Task RunTasks(CancellationToken cancellationToken)
    {
        var scheduler = AvaloniaScheduler.Instance;
        var router = _mainViewModel.Router;
        await _stateService.Initialize();

        if (await _authService.TryAuthFromStorage())
        {
            var tab = Locator.Current.GetService<ChatTabViewModel>()!;
            tab.HostScreen = _mainViewModel;
            
            scheduler.Schedule(_ => router.Navigate.Execute(tab).Subscribe());
        }
        else
        {
            var auth = Locator.Current.GetService<AuthenticationViewModel>()!;
            auth.HostScreen = _mainViewModel;
            
            scheduler.Schedule(_ => router.Navigate.Execute(auth).Subscribe());
        }
    }

    public string? AppName { get; }
    public IImage? AppIcon { get; }
    public object SplashScreenContent { get; }
    public int MinimumShowTime => (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
}