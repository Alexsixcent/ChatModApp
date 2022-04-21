using ChatModApp.Models;
using ChatModApp.Models.Chat.Emotes;
using Refit;

namespace ChatModApp.Services.ApiClients;

public interface IBttvApi
{
    [Get("/emotes/global")]
    public Task<List<BttvUserEmote>> GetGlobalEmotes();

    [Get("/users/twitch/{id}")]
    public Task<ApiResponse<BttvUserEmoteResponse>> GetUserEmotes([AliasAs("id")] int userId);
}