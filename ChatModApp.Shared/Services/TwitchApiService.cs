using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Core.Undocumented;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChatModApp.Services;

public class TwitchApiService
{
    public IObservable<User> UserConnected { get; }

    public Helix Helix => _api.Helix;
    public Undocumented Undocumented => _api.Undocumented;

    private readonly TwitchAPI _api;

    public TwitchApiService(ILoggerFactory apiLoggerFactory, AuthenticationService authService)
    {
        _api = new(apiLoggerFactory);

        UserConnected = authService.AccessTokenChanged
                                   .Select(Connect)
                                   .SelectMany(GetCurrentUser);
    }

    private Unit Connect(string accessToken)
    {
        _api.Settings.ClientId = AuthenticationService.ClientId;
        _api.Settings.AccessToken = accessToken;

        return Unit.Default;
    }

    private async Task<User> GetCurrentUser(Unit _, CancellationToken cancellation)
    {
        var res = await _api.Helix.Users.GetUsersAsync();
        return res.Users.Single();
    }
}