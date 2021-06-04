using System;

namespace ChatModApp.Models.Emotes
{
    public class TwitchEmote : IEmote
    {
        public TwitchEmote(int id, string code)
        {
            Id = id;
            Code = code;
        }

        public string Code { get; }
        public int Id { get; }
        public Uri Uri => new($"http://static-cdn.jtvnw.net/emoticons/v1/{Id}/1.0");
    }

    public class TwitchSubEmote : TwitchEmote
    {
        public TwitchSubEmote(int id, string code) : base(id, code)
        {
        }
    }

    public class TwitchGlobalEmote : TwitchEmote
    {
        public TwitchGlobalEmote(int id, string code) : base(id, code)
        {
        }
    }
}