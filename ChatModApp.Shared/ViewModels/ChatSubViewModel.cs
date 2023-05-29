using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat;
using ReactiveUI;

namespace ChatModApp.Shared.ViewModels;

public class ChatSubViewModel : ReactiveObject, IChatMessage
{
    public required string Id { get; init; }
    public required ITwitchChannel Channel { get; init; }
    public bool IsStripped => true;
    public required string Username { get; init; }
    public required string Plan { get; init; }
    public required string Streak { get; init; }
    public required int Months { get; init; }
    public required string Parsed { get; init; }
    public ChatMessageViewModel? Message { get; init; }
}