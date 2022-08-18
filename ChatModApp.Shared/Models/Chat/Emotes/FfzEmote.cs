﻿using System.Text.Json.Serialization;
using ChatModApp.Shared.Tools.Extensions;

namespace ChatModApp.Shared.Models.Chat.Emotes;

public class FfzEmote : IEmote
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Code { get; set; }

    [JsonPropertyName("urls")]
    public IDictionary<int, Uri> Urls { get; set; }
    
    [JsonPropertyName("owner")]
    public User Owner { get; set; }
    
    public string Provider => "FrankerFaceZ";
    public string? Description => $"By: {Owner.DisplayName}";
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

public class FfzEmoteSet
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("emoticons")]
    public IEnumerable<FfzEmote> Emoticons { get; set; }
}