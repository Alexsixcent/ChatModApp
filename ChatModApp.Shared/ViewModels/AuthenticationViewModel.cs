using System.Reactive;
using System.Reactive.Disposables;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.Shared.ViewModels;

public class WebNavigatedAction
{
    public WebNavigatedAction(Uri uri, Action cancelAction)
    {
        Uri = uri;
        Cancel = cancelAction;
    }

    public Uri Uri { get; }
    public Action Cancel { get; }
}

public class AuthenticationViewModel : ReactiveObject, IRoutableViewModel, IActivatableViewModel
{
    public IScreen? HostScreen { get; set; }
    public string UrlPathSegment => "auth";

    public Uri AuthUri { get; private set; }
    public readonly ReactiveCommand<WebNavigatedAction, Unit> AuthCompleteCommand;

    [Reactive]
    public bool UsingEmbedBrowser { get; set; } = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    private readonly AuthenticationService _authService;
    private readonly ChatTabViewModel _chatTabs;
    private TwitchAuthQueryParams _queryParams;


    public AuthenticationViewModel(AuthenticationService authService, ChatTabViewModel chatTabs)
    {
        _authService = authService;
        _chatTabs = chatTabs;
        Activator = new();
        AuthCompleteCommand = ReactiveCommand.Create<WebNavigatedAction>(AuthComplete);
        
        this.WhenActivated(() =>
        {
            (AuthUri, _queryParams) = authService.GenerateAuthUri();
            return new CompositeDisposable();
        });
    }


    private void AuthComplete(WebNavigatedAction action)
    {
        if (!_authService.AuthFromCallbackUri(action.Uri))
            return;

        action.Cancel();
        _chatTabs.HostScreen = HostScreen;
        HostScreen?.Router.NavigateAndReset.Execute(_chatTabs).Subscribe();
    }

    public ViewModelActivator Activator { get; }
}