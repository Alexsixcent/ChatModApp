using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Undocumented;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChatModApp.Shared.Services;

public class TwitchApiService : ReactiveObject
{
    [ObservableAsProperty]
    public User? CurrentUser { get; }
    public IObservable<User> UserConnected { get; }

    public Helix Helix => _api.Helix;
    public Undocumented Undocumented => _api.Undocumented;

    private const string ClientId = "110gs3dzgr2bj3ask88vqi7mnczk02";
    private static readonly ApiSettings ApiSettings = new() { ClientId = ClientId };
    
    private readonly TwitchAPI _api;

    public TwitchApiService(ILoggerFactory apiLoggerFactory, AuthenticationService authService)
    {
        _api = new(apiLoggerFactory, settings: ApiSettings);

        UserConnected = authService.AccessTokenChanged
                                   .Select(Connect)
                                   .SelectMany(GetCurrentUser);

        UserConnected.ToProperty(this, s => s.CurrentUser, default(User));
    }



    private Unit Connect(string accessToken)
    {
        _api.Settings.AccessToken = accessToken;

        return Unit.Default;
    }

    /// <summary>
    /// Used to validate an access token without using an existing instance of the TwitchApiService
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static Task<ValidateAccessTokenResponse?> ValidateAccessToken(string? token)
    {
        var logFac = Locator.Current.GetService<ILoggerFactory>();
        var api = new TwitchAPI(logFac, settings: ApiSettings);
        
        return api.Auth.ValidateAccessTokenAsync(token);
    }
    
    public static Uri GetTokenAuthUri(string redirect, IEnumerable<AuthScopes> scopes, in Guid state)
    {
        var logFac = Locator.Current.GetService<ILoggerFactory>();
        var api = new TwitchAPI(logFac, settings: ApiSettings);
        
        var uri = api.Auth.GetAuthorizationCodeUrl(redirect, scopes, state: state.ToString("N"), clientId: ClientId);
        uri = uri.Replace("response_type=code", "response_type=token", StringComparison.Ordinal);
        return new(uri);
    }

    private async Task<User> GetCurrentUser(Unit _, CancellationToken cancellation)
    {
        var res = await _api.Helix.Users.GetUsersAsync();
        return res.Users.Single();
    }
}