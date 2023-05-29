namespace ChatModApp.Shared.Models;

public interface ITwitchUser
{
    string Id { get; }
    string Login { get; }
    string DisplayName { get; }
}