namespace ChatModApp.Shared.Models.Chat.Emotes;

public interface IEmote : IImageFrag
{
    string Code { get; }
    string Provider { get; }
    string? Description { get; }
}

public interface IMemberEmote : IEmote
{
    string MemberChannel { get; set; }
}

public interface IGlobalEmote : IEmote
{ }