using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ChatModApp.Models;
using ReactiveUI;
using Tools.Extensions;

namespace ChatModApp.Services
{
    public class AuthenticationService
    {
        public const string ClientId = "110gs3dzgr2bj3ask88vqi7mnczk02";
        private const string RedirectUri = "http://localhost:3000/callback";


        public string TwitchAccessToken
        {
            get => RxApp.SuspensionHost.GetAppState<AppState>().TwitchAccessToken;
            private set => RxApp.SuspensionHost.GetAppState<AppState>().TwitchAccessToken = value;
        }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(TwitchAccessToken);
        public IEnumerable<TwitchAuthScope> Scopes { get; }





        public AuthenticationService()
        {
            Scopes = new List<TwitchAuthScope>
            {
                TwitchAuthScope.ChatRead, 
                TwitchAuthScope.ChatEdit
            };
        }

        public (Uri Uri, TwitchAuthQueryParams QueryParams) GenerateAuthUri()
        {
            var queryParams = new TwitchAuthQueryParams
            {
                ClientId = ClientId,
                ResponseType = TwitchAuthResponseType.Token,
                RedirectUri = new Uri(RedirectUri),
                Scopes = Scopes,
                ForceVerify = false,
                State = Guid.NewGuid().ToString("N")
            };
            return (new Uri("https://id.twitch.tv/oauth2/authorize").AddQueries(queryParams),
                queryParams);
        }

        public bool AuthFromCallbackUri(Uri callbackUri)
        {
            if (!callbackUri.AbsoluteUri.Contains(RedirectUri))
                return false;

            var accessToken = HttpUtility.ParseQueryString(callbackUri.Fragment.Substring(1))["access_token"];

            if (string.IsNullOrWhiteSpace(accessToken))
                return false;

            TwitchAccessToken = accessToken;

            return true;
        }
    }
}