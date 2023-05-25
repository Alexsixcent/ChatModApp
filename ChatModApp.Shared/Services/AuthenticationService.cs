using System.Reactive.Linq;
using System.Web;
using ChatModApp.Shared.Tools;
using TwitchLib.Api.Core.Enums;

namespace ChatModApp.Shared.Services;

public class AuthenticationService
{
    private readonly AppState _state;
    
    
    public string? TwitchAccessToken
    {
        get => _state.TwitchAccessToken;
        private set => _state.TwitchAccessToken = value;
    }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(TwitchAccessToken);
    public IObservable<string> AccessTokenChanged { get; }

    private event EventHandler<string>? AccessTokenChangedEvent;

    public AuthenticationService(AppState state)
    {
        _state = state;
        
        var connectable = Observable.FromEventPattern<string>(h => AccessTokenChangedEvent += h,
                                                              h => AccessTokenChangedEvent -= h)
                                    .Select(pattern => pattern.EventArgs)
                                    .Replay(1);

        connectable.Connect();
        AccessTokenChanged = connectable;
    }
    
    public static (Uri Uri, Guid state) GenerateAuthUri(Uri? redirectUri = null)
    {
        var state = Guid.NewGuid();
        var uri = TwitchApiService.GetTokenAuthUri((redirectUri ?? new("https://localhost/")).AbsoluteUri,
                                         new[]
                                         {
                                             AuthScopes.Chat_Read,
                                             AuthScopes.Chat_Edit,

                                             AuthScopes.User_Subscriptions,
                                             
                                             AuthScopes.Helix_Moderator_Read_Chatters
                                         }, state);

        return (uri, state);
    }

    public async Task<bool> TryAuthFromStorage()
    {
        var res = await TryAuthFromToken(_state.TwitchAccessToken);
        if (!res)
            _state.TwitchAccessToken = null;

        return res;
    }

    public async Task<bool> TryAuthFromCallbackUri(Uri callbackUri)
    {
        if (string.IsNullOrWhiteSpace(callbackUri.Fragment))
            return false;

        var accessToken = HttpUtility.ParseQueryString(callbackUri.Fragment[1..])["access_token"];

        return await TryAuthFromToken(accessToken);
    }

    public async Task<bool> TryAuthFromToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return false;

        var res = await TwitchApiService.ValidateAccessToken(accessToken);
        if (res is null)
            return false;

        TwitchAccessToken = accessToken;
        AccessTokenChangedEvent?.Invoke(this, accessToken);
        return true;
    }
}