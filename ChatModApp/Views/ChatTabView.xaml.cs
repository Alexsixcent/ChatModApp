using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls.Primitives;
using ChatModApp.ViewModels;
using Microsoft.UI.Xaml.Controls;
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
                this.Bind(ViewModel, vm => vm.ChatTabs, v => v.TabView.TabItemsSource)
                    .DisposeWith(disposables);

                this.Bind(ViewModel, vm => vm.ChannelNameSubmit, v => v.FlyoutTextBox.Text)
                    .DisposeWith(disposables);

                this.BindCommand(ViewModel, vm => vm.AddTabCommand, v => v.FlyoutSubmitButton,
                        vm => vm.ChannelNameSubmit)
                    .DisposeWith(disposables);

                Observable.FromEventPattern<TabViewTabCloseRequestedEventArgs>(TabView,
                        nameof(TabView.TabCloseRequested))
                    .Select(pattern => pattern.EventArgs.Item)
                    .InvokeCommand(ViewModel, vm => vm.CloseTabCommand)
                    .DisposeWith(disposables);

                TabView.AddTabButtonClick += (sender, args) => FlyoutBase.ShowAttachedFlyout(sender);
            });
        }
    }
}