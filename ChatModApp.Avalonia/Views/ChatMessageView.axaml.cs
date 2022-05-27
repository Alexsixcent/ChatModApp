using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views;

public partial class ChatMessageView : ReactiveUserControl<ChatMessageViewModel>
{
    public ChatMessageView()
    {
        this.WhenActivated(disposable =>
        {
        });
        InitializeComponent();
    }
}