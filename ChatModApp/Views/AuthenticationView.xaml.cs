using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using ChatModApp.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views
{
    public class AuthenticationViewBase : ReactivePage<AuthenticationViewModel> { }

    /// <summary>
    /// The authentication page.
    /// </summary>
    public sealed partial class AuthenticationView
    {
        public AuthenticationView()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, vm => vm.AuthUri, v => v.AuthWebView.Source)
                    .DisposeWith(disposable);

                AuthWebView.Events()
                    .NavigationStarting
                    .Select(args => args.args)
                    .InvokeCommand(ViewModel.AuthCompleteCommand)
                    .DisposeWith(disposable);
            });
        }
    }
}
