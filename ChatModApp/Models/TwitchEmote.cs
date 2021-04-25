using System;

namespace ChatModApp.Models
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
        public int Height => 28;
        public int Width => 28;
    }
}