using System.Reactive.Disposables;
using ChatModApp.ViewModels;
using ReactiveUI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ChatModApp.Views
{

    public class ChatTabPromptViewBase : ReactivePage<ChatTabPromptViewModel> { }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatTabPromptView
    {
        public ChatTabPromptView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.Bind(ViewModel, vm => vm.ChannelField, v => v.ChannelNameBox.Text)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel, vm => vm.OpenCommand, v => v.OpenButton, vm => vm.ChannelField)
                    .DisposeWith(disposables);
            });
        }
    }
}
