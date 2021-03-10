using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.ViewModels
{
    public class ChatTabItemViewModel : ReactiveObject
    {
        [Reactive]
        public string Title { get; set; }
        public ChatViewModel Chat { get; }


        public ChatTabItemViewModel(ChatViewModel chatViewModel)
        {
            Title = "ChatTab";
            Chat = chatViewModel;
        }
    }
}