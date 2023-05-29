using System.Text.Json.Serialization;
using ChatModApp.Shared.Tools.Extensions;

namespace ChatModApp.Shared.Models.Chat.Emotes;

public abstract class FfzEmote : IEmote
{
    public abstract int Id { get; set; }
    public abstract string Code { get; set; }
    public abstract IDictionary<int, Uri> Urls { get; set; }
    public abstract User Owner { get; set; }
    
    public string Provider => "FrankerFaceZ";
    public string Description => $"By: {Owner.DisplayName}";
    public Uri Small => Urls.FirstOrDefault().Value.RewriteHttps();
    public Uri Medium => Urls.TryGetValue(2, out var value) ? value.RewriteHttps() : Small;
    public Uri Large => Urls.TryGetValue(4, out var value) ? value.RewriteHttps() : Medium;
    
    public class User
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        
        public string Name { get; set; }
        
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }
}

public sealed class FfzGlobalEmote : FfzEmote, IGlobalEmote
{
    [JsonPropertyName("id")]
    public override int Id { get; set; }

    [JsonPropertyName("name")]
    public override string Code { get; set; }

    [JsonPropertyName("urls")]
    public override IDictionary<int, Uri> Urls { get; set; }
    
    [JsonPropertyName("owner")]
    public override User Owner { get; set; }
}

public class FfzUserEmote : FfzEmote, IMemberEmote
{
    [JsonPropertyName("id")]
    public override int Id { get; set; }

    [JsonPropertyName("name")]
    public override string Code { get; set; }

    [JsonPropertyName("urls")]
    public override IDictionary<int, Uri> Urls { get; set; }
    
    [JsonPropertyName("owner")]
    public override User Owner { get; set; }
    
    public ITwitchUser MemberChannel { get; set; }
}


public sealed class FfzEmoteSet<TEmote> where TEmote: FfzEmote
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("emoticons")]
    public IEnumerable<TEmote> Emoticons { get; set; }
}