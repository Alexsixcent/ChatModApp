using System;
using System.Net.Http;
using ChatModApp.Models;
using Refit;

namespace ChatModApp.Services.ApiClients
{
    public interface IAuthApi
    {
        [Post("/oauth2/authorize")]
        [QueryUriFormat(UriFormat.Unescaped)]
        public IObservable<HttpResponseMessage> Authorize([Query]TwitchAuthQueryParams queryParams);
    }
}