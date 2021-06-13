using ChatModApp.Models.Chat.Emotes;

namespace ChatModApp.Models.Chat.Fragments
{
    public class EmoteFragment : IMessageFragment
    {
        public IEmote Emote { get; }

        public EmoteFragment(IEmote emote)
        {
            Emote = emote;
        }
    }
}