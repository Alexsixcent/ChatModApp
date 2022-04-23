using System.Reactive.Linq;
using System.Web;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Tools;
using ChatModApp.Shared.Tools.Extensions;

namespace ChatModApp.Shared.Services;

public class AuthenticationService
{
    private readonly AppState _state;
    public const string ClientId = "110gs3dzgr2bj3ask88vqi7mnczk02";

    public string? TwitchAccessToken
    {
        get => _state.TwitchAccessToken; 
        private set=> _state.TwitchAccessToken = value; 
    }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(TwitchAccessToken);
    public IObservable<string> AccessTokenChanged { get; }
    public IEnumerable<TwitchAuthScope> Scopes { get; }


    private const string RedirectUri = "http://localhost:3000/callback";
    private event EventHandler<string>? AccessTokenChangedEvent;

    public AuthenticationService(AppState state)
    {
        _state = state;
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

    public async Task<bool> TryAuthFromStorage()
    {
        if (string.IsNullOrWhiteSpace(_state.TwitchAccessToken))
            return false;

        var res = await TwitchApiService.ValidateAccessToken(_state.TwitchAccessToken);
        if (res is not null)
            return true;

        _state.TwitchAccessToken = null;
        return false;
    }

    public bool AuthFromCallbackUri(Uri callbackUri)
    {
        if (!callbackUri.AbsoluteUri.StartsWith(RedirectUri))
            return false;

        var accessToken = HttpUtility.ParseQueryString(callbackUri.Fragment.Substring(1))["access_token"];

        if (string.IsNullOrWhiteSpace(accessToken))
            return false;

        TwitchAccessToken = accessToken;
        AccessTokenChangedEvent?.Invoke(this, accessToken);

        return true;
    }
}