using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Tools;
using TwitchLib.Api;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.V5;

namespace ChatModApp.Services
{
    public class TwitchApiService : IService
    {
        public IObservable<User> Connected { get; }

        public Helix Helix => _api.Helix;
        public V5 V5 => _api.V5;

        private readonly TwitchAPI _api;

        public TwitchApiService(AuthenticationService authService)
        {
            _api = new TwitchAPI();

            var o = authService.AccessTokenChanged
                .Select(Connect);

            Connected = o.Select(_ => GetCurrentUser().ToObservable())
                .Concat();

            o.Subscribe();
        }

        public Task Initialize() => Task.CompletedTask;

        private Unit Connect(string accessToken)
        {
            _api.Settings.ClientId = AuthenticationService.ClientId;
            _api.Settings.AccessToken = accessToken;

            return Unit.Default;
        }

        public async Task<User> GetCurrentUser()
        {
            var res = await _api.Helix.Users.GetUsersAsync();
            return res.Users.Single();
        }
    }
}