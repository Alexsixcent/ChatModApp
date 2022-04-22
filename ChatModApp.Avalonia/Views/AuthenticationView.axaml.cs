using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;
using WebViewControl;

namespace ChatModApp.Views;

public partial class AuthenticationView : ReactiveUserControl<AuthenticationViewModel>
{
    public AuthenticationView()
    {
        this.WhenActivated(disposable =>
        {
            this.OneWayBind(ViewModel, vm => vm.AuthUri, v => v.AuthWebView.Address, uri => uri.AbsoluteUri)
                .DisposeWith(disposable);

            Observable.FromEvent<BeforeNavigateEventHandler, Request>(h => AuthWebView.BeforeNavigate += h,
                                                                      h => AuthWebView.BeforeNavigate -= h)
                      .Select(req => new WebNavigatedAction(new(req.Url), req.Cancel))
                      .ObserveOn(AvaloniaScheduler.Instance)
                      .InvokeCommand(ViewModel, vm => vm.AuthCompleteCommand)
                      .DisposeWith(disposable);
        });
        
        InitializeComponent();
    }
}