using ChatModApp.Shared.Models.Chat.Emotes;

namespace ChatModApp.Shared.Models.Chat.Fragments;

public class EmoteFragment : IMessageFragment
{
    public IEmote Emote { get; }

    public EmoteFragment(IEmote emote)
    {
        Emote = emote;
    }
}