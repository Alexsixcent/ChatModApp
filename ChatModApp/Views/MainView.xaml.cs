using System.Reactive.Disposables;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ChatModApp.ViewModels;
using ReactiveUI;
using Splat;

namespace ChatModApp.Views
{
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
}