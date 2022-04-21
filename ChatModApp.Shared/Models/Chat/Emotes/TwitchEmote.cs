using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace ChatModApp.Shared.Models.Chat.Emotes;

public class TwitchEmote : IEmote
{
    public TwitchEmote(string id, string code, string uri)
    {
        Id = id;
        Code = code;
        Uri = new(uri);
    }

    public TwitchEmote(string id, string code)
        : this(id, code, $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/default/dark/1.0")
    { }

    public TwitchEmote(Emote emote)
        : this(emote.Id, emote.Name, emote.Images.Url1X)
    { }

    public TwitchEmote(TwitchLib.Client.Models.Emote emote)
        : this(emote.Id, emote.Name)
    { }

    public string Id { get; }
    public string Code { get; }
    public Uri Uri { get; }


    public static implicit operator TwitchEmote(Emote e) => new(e);
    public static implicit operator TwitchEmote(TwitchLib.Client.Models.Emote e) => new(e);
}