using ChatModApp.Shared.Models;
using Refit;

namespace ChatModApp.Shared.Services.ApiClients;

public interface IFfzApi
{
    [Get("/set/global")]
    public Task<FfzGlobalEmoteResponse> GetGlobalEmotes();

    [Get("/room/id/{id}")]
    public Task<ApiResponse<FfzUserEmoteResponse>> GetChannelEmotes([AliasAs("id")] int userId);
}