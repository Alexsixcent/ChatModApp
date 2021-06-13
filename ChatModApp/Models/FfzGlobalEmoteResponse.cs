using System.Collections.Generic;
using System.Text.Json.Serialization;
using ChatModApp.Models.Chat.Emotes;

namespace ChatModApp.Models
{
    public class FfzGlobalEmoteResponse
    {
        [JsonPropertyName("default_sets")]
        public IEnumerable<int> DefaultSets { get; set; }

        [JsonPropertyName("sets")]
        public IDictionary<int, FfzEmoteSet> Sets { get; set; }
    }
}