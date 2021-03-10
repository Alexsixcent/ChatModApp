using System.Reactive.Disposables;
using ChatModApp.ViewModels;
using ReactiveUI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

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

                this.BindCommand(ViewModel, vm => vm.SubmitCommand, v => v.SubmitButton)
                    .DisposeWith(disposables);
            });
        }
    }
}