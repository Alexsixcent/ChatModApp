using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace ChatModApp.Views;

public partial class ChatTabView : ReactiveUserControl<ChatTabViewModel>
{
    public ChatTabView()
    {
        this.WhenActivated(disposable =>
        {
            Observable.FromEventPattern<TabViewTabCloseRequestedEventArgs>(TabViewControl,
                                                                           nameof(TabViewControl.TabCloseRequested))
                      .Select(pattern => pattern.EventArgs.Item)
                      .InvokeCommand(ViewModel, vm => vm.CloseTabCommand)
                      .DisposeWith(disposable);

            this.BindCommand(ViewModel, vm => vm.AddTabCommand, v => v.TabViewControl,
                             nameof(TabViewControl.AddTabButtonClick))
                .DisposeWith(disposable);

            this.Bind(ViewModel, vm => vm.OpenedTabIndex, v => v.TabViewControl.SelectedIndex)
                .DisposeWith(disposable);
        });
        InitializeComponent();
    }
}