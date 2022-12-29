using ChatModApp.Shared.Models;
using Refit;

namespace ChatModApp.Shared.Services.ApiClients;

public interface IFfzApi
{
    [Get("/set/global")]
    public IObservable<ApiResponse<FfzGlobalEmoteResponse>> GetGlobalEmotes();

    [Get("/room/id/{id}")]
    public IObservable<ApiResponse<FfzUserEmoteResponse>> GetChannelEmotes([AliasAs("id")] string userId);
}