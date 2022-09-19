using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Tools.Extensions;
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

    public readonly ReactiveCommand<WebNavigatedAction, Unit> AuthCompleteCommand;

    [Reactive] public Uri AuthUri { get; }

    [Reactive] public bool UsingEmbedBrowser { get; set; }

    private readonly AuthenticationService _authService;
    private readonly ChatTabViewModel _chatTabs;
    private TwitchAuthQueryParams _queryParams;


    public AuthenticationViewModel(AuthenticationService authService, BlazorHostingService blazorService,
                                   ChatTabViewModel chatTabs)
    {
        _authService = authService;
        _chatTabs = chatTabs;
        Activator = new();
        UsingEmbedBrowser = BlazorHostingService.IsBlazorAuthDisabled;
        AuthCompleteCommand = ReactiveCommand.CreateFromTask<WebNavigatedAction>(AuthComplete);
        var redirectUri = UsingEmbedBrowser ? null : new Uri(new(blazorService.CurrentHostingUrl!), "auth");
        (AuthUri, _queryParams) = authService.GenerateAuthUri(redirectUri);

        this.WhenActivated(disposable =>
        {
            blazorService.AuthFromBlazor
                         ?.SelectMany(args => authService.TryAuthFromCallbackUri(args.CallbackUri))
                         .Where(b => b)
                         .ObserveOnMainThread()
                         .Subscribe(_ =>
                         {
                             chatTabs.HostScreen = HostScreen;
                             HostScreen?.Router.NavigateAndReset.Execute(chatTabs).Subscribe();
                         }).DisposeWith(disposable);
        });
    }


    private async Task AuthComplete(WebNavigatedAction action)
    {
        if (!await _authService.TryAuthFromCallbackUri(action.Uri))
            return;

        action.Cancel();
        _chatTabs.HostScreen = HostScreen;
        HostScreen?.Router.NavigateAndReset.Execute(_chatTabs).Subscribe();
    }

    public ViewModelActivator Activator { get; }
}