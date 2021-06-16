using System;

namespace ChatModApp.Models.Chat.Emotes
{
    public sealed class EmoteKey
    {
        [Flags]
        public enum EmoteType
        {
            Global = 1 << 0,
            Member = Global << 1,

            Twitch = Member << 1,
            Bttv = Twitch << 1,
            FrankerZ = Bttv << 1,
            Emojis = FrankerZ << 1
        }

        public EmoteType Type { get; }
        public string Code { get; }
        public string Channel { get; }

        public EmoteKey(EmoteType type, IEmote emote)
        {
            Type = type;
            Code = emote.Code;
            Channel = string.Empty;
        }

        public EmoteKey(EmoteType type, IEmote emote, string channel)
        {
            Type = type;
            Code = emote.Code;
            Channel = channel;
        }


        public override bool Equals(object obj) =>
            obj is EmoteKey other && Type == other.Type && Code == other.Code && Channel == other.Channel;

        public override int GetHashCode() => (Type, Code, Channel).GetHashCode();
    }
}