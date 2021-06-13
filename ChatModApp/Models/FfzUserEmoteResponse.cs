using System.Collections.Generic;
using System.Text.Json.Serialization;
using ChatModApp.Models.Chat.Emotes;

namespace ChatModApp.Models
{
    public class FfzUserEmoteResponse
    {
        [JsonPropertyName("room")]
        public FfzEmoteRoom Room { get; set; }

        [JsonPropertyName("sets")]
        public IDictionary<int, FfzEmoteSet> Sets { get; set; }
    }

    public class FfzEmoteRoom
    {
        [JsonPropertyName("twitch_id")]
        public int TwitchId { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}