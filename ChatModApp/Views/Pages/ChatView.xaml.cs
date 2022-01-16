using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using ChatModApp.Models.Chat.Emotes;
using ChatModApp.ViewModels;
using ChatModApp.Views.Controls.ChatEditBox;
using ReactiveUI;
using EventExtensions = Windows.UI.Xaml.EventExtensions;
using WinCore = Windows.UI.Core;

namespace ChatModApp.Views.Pages;

public class ChatViewBase : ReactivePage<ChatViewModel>
{ }

public class SuggestionTemplateSelector : DataTemplateSelector
{
    public DataTemplate EmoteTemplate { get; set; }
    public DataTemplate UsernameTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item switch
        {
            IEmote => EmoteTemplate,
            _ => throw new ArgumentException(nameof(item))
        };
    }
}

public sealed partial class ChatView
{
    public ChatView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Observable.FromEventPattern<SuggestionRequestedEventArgs>(ChatEditBox,
                                                                      nameof(ChatEditBox.SuggestionRequested))
                      .Select(pattern => (pattern.EventArgs.Prefix, pattern.EventArgs.QueryText))
                      .InvokeCommand(ViewModel, vm => vm.SuggestionRequestCommand)
                      .DisposeWith(disposables);

            ChatEditBox.SuggestionChosen += (sender, args) =>
            {
                switch (args.SelectedItem)
                {
                    case IEmote emote:
                        args.DisplayText = emote.Code;
                        args.Image = emote.Uri;
                        break;
                }
            };


            this.OneWayBind(ViewModel, vm => vm.ChatMessages, v => v.MessagesCollection.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.ChatSuggestions, v => v.ChatEditBox.ItemsSource)
                .DisposeWith(disposables);

            Observable.FromEventPattern(ChatEditBox, nameof(ChatEditBox.TextChanged))
                      .Select(_ =>
                      {
                          ChatEditBox.TextDocument.GetText(TextGetOptions.NoHidden, out var text);

                          foreach (var token in ChatEditBox.Tokens)
                          {
                              
                          }
                          
                          return text;
                      })
                      .BindTo(ViewModel, vm =>vm.MessageText)
                      .DisposeWith(disposables);

            var enterDown = EventExtensions.Events(ChatEditBox).KeyUp
                                           .Where(args => args.Key == VirtualKey.Enter && !WinCore.CoreWindow
                                                              .GetForCurrentThread()
                                                              .GetKeyState(
                                                                           VirtualKey.Shift)
                                                              .HasFlag(
                                                                       WinCore
                                                                           .CoreVirtualKeyStates
                                                                           .Down))
                                           .Select(_ => ViewModel?.MessageText);

            SubmitButton.Events().Click.Select(_ => ViewModel?.MessageText).Merge(enterDown)
                        .InvokeCommand(ViewModel, vm => vm.SendMessageCommand!).DisposeWith(disposables);
            
            ChatEditBox.PreviewKeyDown += HandleNewLineSkip;
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