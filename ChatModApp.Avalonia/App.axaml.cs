using System;
using System.IO;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ChatModApp.Shared.Tools;
using ChatModApp.Shared.ViewModels;
using ChatModApp.Views;
using ReactiveUI;
using Splat;

namespace ChatModApp;

public class App : Application
{
    public override void Initialize()
    {
        Bootstrapper.Init(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                       Name!));

        Name = "ChatModApp";
        var suspend = new AutoSuspendHelper(ApplicationLifetime!);
        RxApp.SuspensionHost.CreateNewAppState = () => new AppState();
        RxApp.SuspensionHost.SetupDefaultSuspendResume(new AkavacheSuspensionDriver<AppState>());
        RxApp.SuspensionHost.WhenAnyValue(host => host.AppState)
             .WhereNotNull()
             .Cast<AppState>()
             .Subscribe(state => Locator.CurrentMutable.RegisterConstant(state));
        suspend.OnFrameworkInitializationCompleted();

        //Important since UseDryIocDependencyResolver in ConfigureService resets the scheduler to default
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Locator.Current.GetService<MainViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}