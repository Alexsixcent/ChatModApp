namespace ChatModApp.Shared.Models.Chat;

public interface IChatMessage
{
    string Id { get; }
    ITwitchChannel Channel { get; }
    
    bool IsStripped { get; }
}