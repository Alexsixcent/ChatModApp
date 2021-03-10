using System;
using System.Linq;
using System.Reactive;
using Windows.UI.Xaml.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tools.Extensions;

namespace ChatModApp.ViewModels
{
    public class AuthenticationViewModel : ReactiveObject, IRoutableViewModel
    {
        public IScreen HostScreen { get; set; }
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        [Reactive] public Uri AuthUri { get; private set; }
        public readonly ReactiveCommand<WebViewNavigationCompletedEventArgs, Unit> AuthCompleteCommand;


        private const string ClientId = "110gs3dzgr2bj3ask88vqi7mnczk02";
        private const string RedirectUri = "http://localhost";

        private ChatTabViewModel _chatTab;

        public AuthenticationViewModel(ChatTabViewModel chatTab)
        {
            _chatTab = chatTab;
            AuthCompleteCommand = ReactiveCommand.Create<WebViewNavigationCompletedEventArgs>(AuthComplete);
            AuthUri = GenerateAuthUri();
        }

        private Uri GenerateAuthUri()
        {
            return new Uri("https://id.twitch.tv/oauth2/authorize")
                .AddQuery("client_id", ClientId)
                .AddQuery("redirect_uri", RedirectUri)
                .AddQuery("response_type", "token")
                .AddQuery("scope", "viewing_activity_read")
                .AddQuery("state", Guid.NewGuid().ToString());
        }

        private void AuthComplete(WebViewNavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess || args.Uri.Host != "localhost") 
                return;
            
            var segments = args.Uri.ToString().Split('/');
            var accessToken = segments.Last().Split('&').First().TrimStart("#access_token=");

            RxApp.SuspensionHost.GetAppState<AppState>().TwitchAccessToken = accessToken;

            _chatTab.HostScreen = HostScreen;
            HostScreen.Router.NavigateAndReset.Execute(_chatTab).Subscribe();
        }
    }
}