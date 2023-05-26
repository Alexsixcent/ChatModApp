using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views;

public partial class UserListView : ReactiveUserControl<UserListViewModel>
{
    public UserListView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.ChattersLoadCommand, v => v.UserListRefreshButton)
                .DisposeWith(d);
        });
    }
}