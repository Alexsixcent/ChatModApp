namespace ChatModApp.Shared.Models.Chat.Emotes;

public interface IEmote : IImageFrag
{
    string Code { get; }
    string Provider { get; }
}