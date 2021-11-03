using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.ViewModels;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace ChatModApp.Views.Pages;

public class ChatTabViewBase : ReactivePage<ChatTabViewModel> { }

public sealed partial class ChatTabView
{
    public ChatTabView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.ChatTabs, v => v.TabViewControl.TabItemsSource).DisposeWith(disposables);

            Observable
                .FromEventPattern<
                    TabViewTabCloseRequestedEventArgs>(TabViewControl, nameof(TabViewControl.TabCloseRequested))
                .Select(pattern => pattern.EventArgs.Item).InvokeCommand(ViewModel, vm => vm.CloseTabCommand)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                             vm => vm.AddTabCommand,
                             v => v.TabViewControl,
                             nameof(TabViewControl.AddTabButtonClick)).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.OpenedTabIndex, v => v.TabViewControl.SelectedIndex).DisposeWith(disposables);
        });
    }
}