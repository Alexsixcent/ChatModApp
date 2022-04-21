using System.Text.Json.Serialization;
using ChatModApp.Shared.Models.Chat.Emotes;

namespace ChatModApp.Shared.Models;

public class FfzGlobalEmoteResponse
{
    [JsonPropertyName("default_sets")]
    public IEnumerable<int> DefaultSets { get; set; }

    [JsonPropertyName("sets")]
    public IDictionary<int, FfzEmoteSet> Sets { get; set; }
}