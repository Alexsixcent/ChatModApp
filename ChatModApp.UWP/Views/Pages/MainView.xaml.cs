using System.Reactive.Disposables;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;
using Splat;

namespace ChatModApp.Views.Pages;

public class MainViewBase : ReactivePage<MainViewModel> { }
public sealed partial class MainView
{
    public MainView()
    {
        ViewModel = Locator.Current.GetService<MainViewModel>();

        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Router, v => v.RoutedViewHost.Router)
                .DisposeWith(disposables);
        });
    }
}