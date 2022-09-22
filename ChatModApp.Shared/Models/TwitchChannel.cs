namespace ChatModApp.Shared.Models;

public class TwitchChannel : ITwitchChannel
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Login { get; }

    public TwitchChannel(string id, string displayName, string login)
    {
        Id = id;
        DisplayName = displayName;
        Login = login;
    }
}