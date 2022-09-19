using System;
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
        this.WhenActivated(_ => { });

        InitializeComponent();
    }


    private void WebView_OnBeforeNavigate(Request request)
    {
        var action = new WebNavigatedAction(new(request.Url), request.Cancel);
        Dispatcher.UIThread.Post(() => { ViewModel?.AuthCompleteCommand.Execute(action).Subscribe(); });
    }
}