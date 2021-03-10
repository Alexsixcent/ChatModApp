using System.Reactive.Disposables;
using ChatModApp.ViewModels;
using ReactiveUI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ChatModApp.Views
{
    public class ChatTabViewBase : ReactivePage<ChatTabViewModel>
    {
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatTabView
    {
        public ChatTabView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.ChatTabs, v => v.TabView.TabItemsSource)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel, vm => vm.AddTabCommand, v => v.TabView.AddTabButtonCommand)
                    .DisposeWith(disposables);
            });
        }
    }
}