using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;
using Splat;

namespace ChatModApp.Views;

public enum StatusType
{
    NotAutoScrollingToBottom,
    AutoScrollingToBottom,
    AutoScrollingToBottomButSuppressed
}

public partial class ChatView : ReactiveUserControl<ChatViewModel>, IEnableLogger
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

            Observable.FromEventPattern(ResumeScrollButton, nameof(ResumeScrollButton.Click))
                      .Subscribe(_ =>
                      {
                          if (MessageList.Scroll is ScrollViewer sw) sw.ScrollToEnd();
                      })
                      .DisposeWith(disposable);

            var keyUp = Observable.FromEventPattern<KeyEventArgs>(ChatBox, nameof(ChatBox.KeyUp))
                                  .Select(pattern => pattern.EventArgs);
            var keyDown = Observable.FromEventPattern<KeyEventArgs>(ChatBox, nameof(ChatBox.KeyDown))
                                    .Select(pattern => pattern.EventArgs);

            keyUp.Where(args => args.Key is Key.Enter && args.KeyModifiers is not KeyModifiers.Shift)
                 .Select(_ => ChatBox.Text)
                 .Log(this, $"Sending message in chat of {ViewModel?.Channel}", s => s)
                 .InvokeCommand(ViewModel, vm => vm.SendMessageCommand)
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

            ResumeScrollButton.Opacity = 1;
        }

        // Check if auto scrolling should be unsuppressed
        if (userScrolledToBottom)
        {
            if (_scrollStatus == StatusType.AutoScrollingToBottomButSuppressed)
            {
                _scrollStatus = StatusType.AutoScrollingToBottom;
            }

            ResumeScrollButton.Opacity = 0;
        }

        if (e.ExtentDelta.Y > 0 && _scrollStatus is StatusType.AutoScrollingToBottom)
        {
            sw.ScrollToEnd();
        }
    }
}