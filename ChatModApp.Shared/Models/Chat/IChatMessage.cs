namespace ChatModApp.Shared.Models.Chat;

public interface IChatMessage
{
    string Id { get; }
    ITwitchUser Channel { get; }
    
    bool IsStripped { get; }
}