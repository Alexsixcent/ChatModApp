namespace ChatModApp.Shared.Models;

public interface ITwitchChannel
{
    string Id { get; }
    string DisplayName { get; }
    string Login { get; }
}