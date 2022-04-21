namespace ChatModApp.Models.Chat;

public class TwitchChatBadge : IChatBadge
{
    public ITwitchChannel? Channel { get; }
    public string SetId { get; }
    public string Id { get; }
    public Uri Small { get; }
    public Uri Medium { get; }
    public Uri Large { get; }

    public TwitchChatBadge(ITwitchChannel channel, string setId, string id, Uri small, Uri medium, Uri large)
    {
        Channel = channel;
        SetId = setId;
        Id = id;
        Small = small;
        Medium = medium;
        Large = large;
    }

    public TwitchChatBadge(string setId, string id, Uri small, Uri medium, Uri large)
    {
        SetId = setId;
        Id = id;
        Small = small;
        Medium = medium;
        Large = large;
    }

    public override bool Equals(object obj) =>
        obj is TwitchChatBadge other && Channel == other.Channel && SetId == other.SetId && Id == other.Id;

    public override int GetHashCode() => (Channel, SetId, Id).GetHashCode();
}