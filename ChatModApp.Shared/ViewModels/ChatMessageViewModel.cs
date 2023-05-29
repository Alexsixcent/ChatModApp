using System.Drawing;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Models.Chat.Fragments;
using ReactiveUI;

namespace ChatModApp.Shared.ViewModels;

public class ChatMessageViewModel : ReactiveObject, IChatMessage
{
    public required ITwitchChannel Channel { get; init; }
    public bool IsStripped => false;
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required IEnumerable<IChatBadge> Badges { get; init; }
    public required IEnumerable<IMessageFragment> Message { get; init; }
    public required Color UsernameColor { get; init; }
}