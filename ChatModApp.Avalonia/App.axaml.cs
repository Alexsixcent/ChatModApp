using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ChatModApp.Shared.Tools;
using ChatModApp.Views;
using DryIoc;
using FluentAvalonia.UI.Windowing;
using ReactiveUI;
using Splat;
using AppSplashScreen = ChatModApp.Tools.AppSplashScreen;

namespace ChatModApp;

public class App : Application
{
    private Bootstrapper _bootstrap = null!;
    private AutoSuspendHelper _suspend = null!;

    public override void Initialize()
    {
        _suspend = new(ApplicationLifetime!);

        Name = nameof(ChatModApp);
        _bootstrap = new Bootstrapper(Name)
            .Configure(container =>
            {
                container.Register<IApplicationSplashScreen, AppSplashScreen>(Reuse.Singleton);
                container.Register<MainWindow>(Reuse.Singleton, made: FactoryMethod.ConstructorWithResolvableArguments);
            });

        AvaloniaXamlLoader.Load(this);
    }


    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            RxApp.SuspensionHost.IsLaunchingNew.Subscribe(_ =>
                desktop.MainWindow = Locator.Current.GetService<MainWindow>());
            
            desktop.Startup += DesktopOnStartup;
            desktop.Exit += DesktopOnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void DesktopOnStartup(object? _, ControlledApplicationLifetimeStartupEventArgs args)
    {
        await _bootstrap.Start(args.Args);

        //Important since UseDryIocDependencyResolver in ConfigureService resets the scheduler to default
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        _suspend.OnFrameworkInitializationCompleted();
    }

    private async void DesktopOnExit(object? _, ControlledApplicationLifetimeExitEventArgs e)
    {
        await _bootstrap.Stop();
        _bootstrap.Dispose();
    }
}