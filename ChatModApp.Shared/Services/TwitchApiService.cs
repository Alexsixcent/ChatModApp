using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Splat;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Undocumented;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChatModApp.Shared.Services;

public class TwitchApiService
{
    public IObservable<User> UserConnected { get; }

    public Helix Helix => _api.Helix;
    public Undocumented Undocumented => _api.Undocumented;

    private static readonly ApiSettings ApiSettings = new() { ClientId = AuthenticationService.ClientId };
    
    private readonly TwitchAPI _api;

    public TwitchApiService(ILoggerFactory apiLoggerFactory, AuthenticationService authService)
    {
        _api = new(apiLoggerFactory, settings: ApiSettings);

        UserConnected = authService.AccessTokenChanged
                                   .Select(Connect)
                                   .SelectMany(GetCurrentUser);
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

    private Unit Connect(string accessToken)
    {
        _api.Settings.AccessToken = accessToken;

        return Unit.Default;
    }

    private async Task<User> GetCurrentUser(Unit _, CancellationToken cancellation)
    {
        var res = await _api.Helix.Users.GetUsersAsync();
        return res.Users.Single();
    }
}