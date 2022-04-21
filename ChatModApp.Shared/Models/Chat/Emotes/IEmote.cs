namespace ChatModApp.Shared.Models.Chat.Emotes;

public interface IEmote
{
    string Code { get; }
    Uri Uri { get; }
}