using System.Drawing;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Models.Chat.Fragments;
using ReactiveUI;

namespace ChatModApp.Shared.ViewModels;

public class ChatMessageViewModel : ReactiveObject
{
    public ChatMessageViewModel(string id, string username, IEnumerable<IChatBadge> badges, IEnumerable<IMessageFragment> message, Color usernameColor)
    {
        Id = id;
        Username = username;
        Message = message;
        UsernameColor = usernameColor;
        Badges = badges;
    }

    public string Id { get; }
    public string Username { get; }
    public IEnumerable<IChatBadge> Badges { get; }
    public IEnumerable<IMessageFragment> Message { get; }
    public Color UsernameColor { get; }
}