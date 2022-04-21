using System.Reactive.Linq;
using System.Web;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Tools.Extensions;

namespace ChatModApp.Shared.Services;

public class AuthenticationService
{
    public const string ClientId = "110gs3dzgr2bj3ask88vqi7mnczk02";

    public string TwitchAccessToken { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(TwitchAccessToken);
    public IObservable<string> AccessTokenChanged { get; }
    public IEnumerable<TwitchAuthScope> Scopes { get; }


    private const string RedirectUri = "http://localhost:3000/callback";
    private event EventHandler<string>? AccessTokenChangedEvent;

    public AuthenticationService()
    {
        TwitchAccessToken = string.Empty;
        Scopes = new List<TwitchAuthScope>
        {
            TwitchAuthScope.ChatRead,
            TwitchAuthScope.ChatEdit,

            TwitchAuthScope.UserSubscriptions
        };

        var connectable = Observable.FromEventPattern<string>(
                                                              h => AccessTokenChangedEvent += h,
                                                              h => AccessTokenChangedEvent -= h)
                                    .Select(pattern => pattern.EventArgs)
                                    .Replay(1);

        connectable.Connect();
        AccessTokenChanged = connectable;
    }

    public (Uri Uri, TwitchAuthQueryParams QueryParams) GenerateAuthUri()
    {
        var queryParams = new TwitchAuthQueryParams
        {
            ClientId = ClientId,
            ResponseType = TwitchAuthResponseType.Token,
            RedirectUri = new(RedirectUri),
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
        AccessTokenChangedEvent?.Invoke(this, accessToken);

        return true;
    }
}