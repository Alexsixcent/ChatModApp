namespace ChatModApp.Shared.Models.Chat.Emotes;

public sealed class EmojiEmote : IGlobalEmote
{
    public EmojiEmote(string code, string emojiValue, string emojiGroup, string emojiSubGroup)
    {
        Code = code;
        EmojiValue = emojiValue;
        EmojiGroup = emojiGroup;
        EmojiSubGroup = emojiSubGroup;
    }

    public Uri Small { get; }
    public Uri Medium { get; }
    public Uri Large { get; }
    public string Code { get; }
    public string Provider => "Emoji";
    public string? Description => null;
    
    public string EmojiValue { get; }
    public string EmojiGroup { get; }
    public string EmojiSubGroup { get; }
}