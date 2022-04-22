using Avalonia.Controls.Mixins;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views
{
    public partial class MainWindow : ReactiveWindow<MainViewModel>
    {
        public MainWindow()
        {
            this.WhenActivated(disposable =>
            {
                this.OneWayBind<MainViewModel, MainWindow, RoutingState, RoutingState?>(ViewModel, vm => vm.Router, v => v.RoutedViewHost.Router)
                    .DisposeWith(disposable);
            });
            
            InitializeComponent();
        }
    }
}