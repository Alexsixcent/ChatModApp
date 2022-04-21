using System.Reactive;
using ChatModApp.Models;
using ChatModApp.Services;
using ReactiveUI;

namespace ChatModApp.ViewModels;

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

public class AuthenticationViewModel : ReactiveObject, IRoutableViewModel
{
    public IScreen? HostScreen { get; set; }
    public string UrlPathSegment => "auth";

    public Uri AuthUri { get; }
    public readonly ReactiveCommand<WebNavigatedAction, Unit> AuthCompleteCommand;


    private readonly AuthenticationService _authService;
    private readonly ChatTabViewModel _chatTabs;
    private readonly TwitchAuthQueryParams _queryParams;


    public AuthenticationViewModel(AuthenticationService authService, ChatTabViewModel chatTabs)
    {
        _authService = authService;
        _chatTabs = chatTabs;
        AuthCompleteCommand = ReactiveCommand.Create<WebNavigatedAction>(AuthComplete);

        (AuthUri, _queryParams) = authService.GenerateAuthUri();
    }


    private void AuthComplete(WebNavigatedAction action)
    {
        if (!_authService.AuthFromCallbackUri(action.Uri))
            return;

        action.Cancel();
        _chatTabs.HostScreen = HostScreen;
        HostScreen.Router.NavigateAndReset.Execute(_chatTabs).Subscribe();
    }
}