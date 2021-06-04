using System;
using Refit;

namespace ChatModApp.Models.Emotes
{
    public interface IBttvEmote : IEmote
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string ImageType { get; set; }
    }

    public class BttvUserEmote : IBttvEmote
    {
        [AliasAs("id")]
        public string Id { get; set; }

        [AliasAs("code")]
        public string Code { get; set; }

        [AliasAs("imageType")]
        public string ImageType { get; set; }

        [AliasAs("userId")]
        public string UserId { get; set; }


        public Uri Uri => new($"https://cdn.betterttv.net/emote/{Id}/1x");
    }

    public class BttvSharedEmote : IBttvEmote
    {
        [AliasAs("id")]
        public string Id { get; set; }

        [AliasAs("code")]
        public string Code { get; set; }

        [AliasAs("imageType")]
        public string ImageType { get; set; }

        [AliasAs("user")]
        public BttvUser User { get; set; }

        public Uri Uri => new($"https://cdn.betterttv.net/emote/{Id}/1x");
    }

    public class BttvUser
    {
        [AliasAs("id")]
        public string Id { get; set; }

        [AliasAs("name")]
        public string Name { get; set; }

        [AliasAs("displayName")]
        public string DisplayName { get; set; }

        [AliasAs("providerId")]
        public string ProviderId { get; set; }
    }
}