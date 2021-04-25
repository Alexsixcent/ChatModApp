namespace ChatModApp.Models
{
    public class EmoteFragment : IMessageFragment
    {
        public EmoteFragment(IEmote emote)
        {
            Emote = emote;
        }

        public IEmote Emote { get; }
    }
}