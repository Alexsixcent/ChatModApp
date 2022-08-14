using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace ChatModApp.Shared.Models.Chat.Emotes;

public class TwitchEmote : IEmote
{
    public TwitchEmote(string id, string code, Uri small, Uri medium, Uri large)
    {
        Id = id;
        Code = code;
        Small = small;
        Medium = medium;
        Large = large;
    }

    public TwitchEmote(string id, string code, string small, string medium, string large)
        : this(id, code, new Uri(small), new(medium), new(large))
    { }

    public TwitchEmote(string id, string code)
        : this(id, code, $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/default/dark/1.0",
               $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/default/dark/2.0",
               $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/default/dark/3.0")
    { }

    public TwitchEmote(Emote emote)
        : this(emote.Id, emote.Name, emote.Images.Url1X, emote.Images.Url2X, emote.Images.Url4X)
    { }

    public TwitchEmote(TwitchLib.Client.Models.Emote emote)
        : this(emote.Id, emote.Name)
    { }

    public string Id { get; }
    public string Code { get; }
    public Uri Small { get; }
    public Uri Medium { get; }
    public Uri Large { get; }


    public static implicit operator TwitchEmote(Emote e) => new(e);
    public static implicit operator TwitchEmote(TwitchLib.Client.Models.Emote e) => new(e);
}