using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ChatModApp.ViewModels;
using DynamicData.Alias;
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

                this.OneWayBind(ViewModel, vm => vm.AddTabCommand, v => v.TabView.AddTabButtonCommand)
                    .DisposeWith(disposables);

                Observable.FromEventPattern<TabViewTabCloseRequestedEventArgs>(TabView, nameof(TabView.TabCloseRequested))
                    .Select(pattern => pattern.EventArgs.Item)
                    .InvokeCommand(ViewModel, vm=>vm.CloseTabCommand)
                    .DisposeWith(disposables);
            });
        }
    }
}