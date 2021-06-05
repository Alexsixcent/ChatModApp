using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Tools.Extensions;

namespace ChatModApp.Models.Emotes
{
    public class FfzEmote : IEmote
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Code { get; set; }

        [JsonPropertyName("urls")]
        public IDictionary<int, Uri> Urls { get; set; }


        public Uri Uri => Urls.FirstOrDefault().Value.RewriteHttps();
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
}