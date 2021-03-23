using System.Drawing;
using ReactiveUI;

namespace ChatModApp.ViewModels
{
    public class ChatMessageViewModel : ReactiveObject
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public Color UsernameColor { get; set; }
    }
}