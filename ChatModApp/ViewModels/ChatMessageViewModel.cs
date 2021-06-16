using System.Collections.Generic;
using System.Drawing;
using ChatModApp.Models.Chat.Fragments;
using ReactiveUI;

namespace ChatModApp.ViewModels
{
    public class ChatMessageViewModel : ReactiveObject
    {
        public ChatMessageViewModel(string id, string username, IEnumerable<IMessageFragment> message, Color usernameColor)
        {
            Id = id;
            Username = username;
            Message = message;
            UsernameColor = usernameColor;
        }

        public string Id { get; }
        public string Username { get; }
        public IEnumerable<IMessageFragment> Message { get; }
        public Color UsernameColor { get; }
    }
}