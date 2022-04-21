using ChatModApp.Shared.Models;
using Refit;

namespace ChatModApp.Shared.Services.ApiClients;

public interface IAuthApi
{
    [Post("/oauth2/authorize")]
    [QueryUriFormat(UriFormat.Unescaped)]
    public IObservable<HttpResponseMessage> Authorize([Query]TwitchAuthQueryParams queryParams);
}