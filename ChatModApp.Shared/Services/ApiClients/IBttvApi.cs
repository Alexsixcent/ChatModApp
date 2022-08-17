using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using Refit;

namespace ChatModApp.Shared.Services.ApiClients;

public interface IBttvApi
{
    [Get("/emotes/global")]
    public Task<List<BttvGlobalEmote>> GetGlobalEmotes();

    [Get("/users/twitch/{id}")]
    public Task<ApiResponse<BttvUserEmoteResponse>> GetUserEmotes([AliasAs("id")] int userId);
}