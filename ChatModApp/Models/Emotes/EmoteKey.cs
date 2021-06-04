using System;

namespace ChatModApp.Models.Emotes
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


        public override bool Equals(object obj)
        {
            if (obj is EmoteKey other)
            {
                return Type == other.Type && Code == other.Code && Channel == other.Channel;
            }

            return false;
        }

        public override int GetHashCode() => Tuple.Create(Type, Code, Channel).GetHashCode();
    }
}