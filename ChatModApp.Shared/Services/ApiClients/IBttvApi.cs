using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using Refit;

namespace ChatModApp.Shared.Services.ApiClients;

public interface IBttvApi
{
    [Get("/emotes/global")]
    public IObservable<List<BttvGlobalEmote>> GetGlobalEmotes();

    [Get("/users/twitch/{id}")]
    public IObservable<ApiResponse<BttvUserEmoteResponse>> GetUserEmotes([AliasAs("id")] string userId);
}