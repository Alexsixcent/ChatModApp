using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using ChatModApp.ViewModels;
using ReactiveUI;
using EventExtensions = Windows.UI.Xaml.EventExtensions;
using WinCore = Windows.UI.Core;

namespace ChatModApp.Views.Pages;

public class ChatViewBase : ReactiveUserControl<ChatViewModel> { }

public sealed partial class ChatView
{
    public ChatView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ChatMessages, v => v.MessagesCollection.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.MessageText, v => v.ChatBox.Text).DisposeWith(disposables);

            var enterDown = EventExtensions.Events(ChatBox).KeyUp
                                           .Where(args => args.Key == VirtualKey.Enter && !WinCore.CoreWindow
                                                                                                  .GetForCurrentThread()
                                                                                                  .GetKeyState(
                                                                                                      VirtualKey.Shift)
                                                                                                  .HasFlag(
                                                                                                      WinCore
                                                                                                          .CoreVirtualKeyStates
                                                                                                          .Down)).Select(
                                               _ => ChatBox.Text);

            SubmitButton.Events().Click.Select(_ => ChatBox.Text).Merge(enterDown)
                        .InvokeCommand(ViewModel, vm => vm.SendMessageCommand).DisposeWith(disposables);

            ChatBox.PreviewKeyDown += HandleNewLineSkip;
        });
    }

    private static void HandleNewLineSkip(object sender, KeyRoutedEventArgs e)
    {
        // NOTE - AcceptsReturn is set to true in XAML.
        if (e.Key != VirtualKey.Enter) return;
        // If SHIFT is pressed, this next IF is skipped over, so the
        //     default behavior of "AcceptsReturn" is used.
        if (!WinCore.CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift)
                    .HasFlag(WinCore.CoreVirtualKeyStates.Down))
            // Mark the event as handled, so the default behavior of 
            //    "AcceptsReturn" is not used.
            e.Handled = true;
    }
}