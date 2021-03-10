using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using ChatModApp.ViewModels;
using ReactiveUI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ChatModApp.Views
{
    public class AuthenticationViewBase : ReactivePage<AuthenticationViewModel> { }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
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
                    .NavigationCompleted
                    .Select(args => args.args)
                    .InvokeCommand(ViewModel.AuthCompleteCommand)
                    .DisposeWith(disposable);
            });
        }
    }
}
