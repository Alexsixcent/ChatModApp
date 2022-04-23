using System;
using System.IO;
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

namespace ChatModApp
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            var suspend = new AutoSuspendHelper(ApplicationLifetime!);
            RxApp.SuspensionHost.CreateNewAppState = () => new AppState();
            RxApp.SuspensionHost.SetupDefaultSuspendResume(new AkavacheSuspensionDriver<AppState>());
            suspend.OnFrameworkInitializationCompleted();
            
            Bootstrapper.Init(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name!));

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
}