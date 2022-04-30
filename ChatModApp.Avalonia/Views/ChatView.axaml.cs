using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views;

public enum StatusType
{
    NotAutoScrollingToBottom,
    AutoScrollingToBottom,
    AutoScrollingToBottomButSuppressed
}

public partial class ChatView : ReactiveUserControl<ChatViewModel>
{
    private StatusType _scrollStatus = StatusType.AutoScrollingToBottom;

    public ChatView()
    {
        this.WhenActivated(disposable =>
        {
            this.BindCommand(ViewModel, vm => vm.ChattersLoadCommand, v => v.UserListRefreshButton)
                .DisposeWith(disposable);

            this.WhenAnyValue(v => v.MessageList.Scroll)
                .WhereNotNull()
                .Subscribe(scrollable =>
                {
                    if (scrollable is ScrollViewer sw) sw.ScrollChanged += OnScrollChanged;
                })
                .DisposeWith(disposable);
        });
        InitializeComponent();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer sw)
            return;

        var userScrolledToBottom = (sw.Offset.Y + sw.Viewport.Height) > (sw.Extent.Height - 1.0);
        var userScrolledUp = e.OffsetDelta.Y < 0;

        // Check if auto scrolling should be suppressed
        if (userScrolledUp && !userScrolledToBottom)
        {
            if (_scrollStatus == StatusType.AutoScrollingToBottom)
            {
                _scrollStatus = StatusType.AutoScrollingToBottomButSuppressed;
            }
        }

        // Check if auto scrolling should be unsuppressed
        if (userScrolledToBottom)
        {
            if (_scrollStatus == StatusType.AutoScrollingToBottomButSuppressed)
            {
                _scrollStatus = StatusType.AutoScrollingToBottom;
            }
        }

        if (e.ExtentDelta.Y > 0 && _scrollStatus is StatusType.AutoScrollingToBottom)
        {
            sw.ScrollToEnd();
        }
    }
}