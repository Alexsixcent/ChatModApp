using System.Collections.Generic;
using System.Drawing;
using ChatModApp.Models.Chat;
using ChatModApp.Models.Chat.Fragments;
using ReactiveUI;

namespace ChatModApp.ViewModels;

public class ChatMessageViewModel : ReactiveObject
{
    public string Id { get; }
    public string Username { get; }
    public IEnumerable<IChatBadge> Badges { get; }
    public IEnumerable<IMessageFragment> Message { get; }
    public Color UsernameColor { get; }

    public ChatMessageViewModel(string id,
                                string username,
                                IEnumerable<IChatBadge> badges,
                                IEnumerable<IMessageFragment> message,
                                Color usernameColor)
    {
        Id = id;
        Username = username;
        Message = message;
        UsernameColor = usernameColor;
        Badges = badges;
    }
}