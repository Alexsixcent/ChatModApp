using System.Collections.Generic;
using System.Drawing;
using ChatModApp.Models.Chat.Fragments;
using ReactiveUI;

namespace ChatModApp.ViewModels
{
    public class ChatMessageViewModel : ReactiveObject
    {
        public string Username { get; set; }
        public IEnumerable<IMessageFragment> Message { get; set; }
        public Color UsernameColor { get; set; }
    }
}