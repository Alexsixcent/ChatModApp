namespace ChatModApp.Shared.Models;

public sealed record TwitchUser(string Id, string Login, string DisplayName) : ITwitchUser
{
    public bool Equals(TwitchUser? other) => ReferenceEquals(this, other) || Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}