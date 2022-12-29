namespace ChatModApp.Shared.Models;

public sealed record TwitchChannel(string Id, string DisplayName, string Login) : ITwitchChannel
{
    public bool Equals(TwitchChannel? other) => ReferenceEquals(this, other) || Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}