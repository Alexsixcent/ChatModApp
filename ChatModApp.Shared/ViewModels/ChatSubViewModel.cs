using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat;
using ReactiveUI;

namespace ChatModApp.Shared.ViewModels;

public sealed class ChatSubViewModel : ReactiveObject, IChatMessage
{
    public bool IsStripped => true;
    public required string Id { get; init; }
    public required ITwitchUser Channel { get; init; }
    public required ITwitchUser User { get; init; }
    public required string Plan { get; init; }
    public required string Streak { get; init; }
    public required int Months { get; init; }
    public required string Parsed { get; init; }
    public ChatMessageViewModel? Message { get; init; }
}