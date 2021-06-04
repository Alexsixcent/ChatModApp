using System.Collections.Generic;
using System.Threading.Tasks;
using ChatModApp.Models;
using ChatModApp.Models.Emotes;
using Refit;

namespace ChatModApp.Services.ApiClients
{
    public interface IBttvApi
    {
        [Get("/emotes/global")]
        public Task<List<BttvUserEmote>> GetGlobalEmotes();

        [Get("/users/twitch/{id}")]
        public Task<BttvUserEmoteResponse> GetUserEmotes([AliasAs("id")] int userId);
    }
}