using System.Reactive.Disposables;
using ChatModApp.Tools;
using ChatModApp.ViewModels;
using ReactiveUI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ChatModApp.Views
{
    public class ChatTabItemViewBase : ReactiveTabViewItem<ChatTabItemViewModel> { }
    public sealed partial class ChatTabItemView
    {
        public ChatTabItemView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.Title, v => v.Header)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel, vm => vm.Chat, v => v.Content)
                    .DisposeWith(disposables);
            });
        }
    }
}
