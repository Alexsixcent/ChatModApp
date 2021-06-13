using System;

namespace ChatModApp.Models.Chat.Emotes
{
    public class TwitchEmote : IEmote
    {
        public TwitchEmote(string id, string code)
        {
            Id = id;
            Code = code;
        }

        public string Code { get; }
        string Id { get; }
        public Uri Uri => new($"http://static-cdn.jtvnw.net/emoticons/v1/{Id}/1.0");
    }

    public class TwitchSubEmote : TwitchEmote
    {
        public TwitchSubEmote(string id, string code) : base(id, code)
        {
        }
    }

    public class TwitchGlobalEmote : TwitchEmote
    {
        public TwitchGlobalEmote(string id, string code) : base(id, code)
        {
        }
    }
}