using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views.Pages;

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
                       .Select(tuple => new WebNavigatedAction(tuple.args.Uri, () => tuple.args.Cancel = true))
                       .InvokeCommand(ViewModel, vm => vm.AuthCompleteCommand)
                       .DisposeWith(disposable);
        });
    }
}