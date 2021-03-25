using System;
using System.Reactive;
using Windows.UI.Xaml.Controls;
using ChatModApp.Models;
using ChatModApp.Services;
using ReactiveUI;

namespace ChatModApp.ViewModels
{
    public class AuthenticationViewModel : ReactiveObject, IRoutableViewModel
    {
        public IScreen? HostScreen { get; set; }
        public string UrlPathSegment { get; } = "auth";

        public Uri AuthUri { get; }
        public readonly ReactiveCommand<WebViewNavigationStartingEventArgs, Unit> AuthCompleteCommand;


        private readonly AuthenticationService _authService;
        private readonly ChatTabViewModel _chatTabs;
        private readonly TwitchAuthQueryParams _queryParams;


        public AuthenticationViewModel(AuthenticationService authService, ChatTabViewModel chatTabs)
        {
            _authService = authService;
            _chatTabs = chatTabs;
            AuthCompleteCommand = ReactiveCommand.Create<WebViewNavigationStartingEventArgs>(AuthComplete);

            (AuthUri, _queryParams) = authService.GenerateAuthUri();
        }


        private void AuthComplete(WebViewNavigationStartingEventArgs args)
        {
            if (!_authService.AuthFromCallbackUri(args.Uri))
                return;

            args.Cancel = true;
            _chatTabs.HostScreen = HostScreen;
            HostScreen.Router.NavigateAndReset.Execute(_chatTabs).Subscribe();
        }
    }
}