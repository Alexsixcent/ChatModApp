using System.Reactive.Disposables;
using ChatModApp.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views
{
    public class ChatViewBase : ReactiveUserControl<ChatViewModel>
    {
    }

    public sealed partial class ChatView
    {
        public ChatView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.ChatMessages, v => v.ChatList.ItemsSource)
                    .DisposeWith(disposables);
            });
        }
    }
}