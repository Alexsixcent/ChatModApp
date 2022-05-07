using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.ReactiveUI;
using ChatModApp.Controls;
using ChatModApp.Shared.Models.Chat.Fragments;
using ChatModApp.Shared.ViewModels;
using ChatModApp.Tools;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using Splat;

namespace ChatModApp.Views;

public partial class ChatMessageView : ReactiveUserControl<ChatMessageViewModel>, IEnableLogger
{
    private static TextBlock UsernameColon => new()
    {
        Text = ": ",
        TextWrapping = TextWrapping.NoWrap,
        TextAlignment = TextAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new(0, 0, 0, 1)
    };

    public ChatMessageView()
    {
        this.WhenActivated(disposable =>
        {
            //TODO: Rework when RichTextBlock and Inlines support comes in

            var badges = ViewModel?.Badges.Select(badge => GetImageFromUri(badge.Small)) ?? Enumerable.Empty<Control>();
            var username = new TextBlock
            {
                Text = ViewModel?.Username,
                TextWrapping = TextWrapping.NoWrap,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(1, 0, 0, 0),
                FontWeight = FontWeight.Bold,
                Foreground = new ImmutableSolidColorBrush(ViewModel?.UsernameColor.ToUiColor() ?? Colors.White)
            };
            var frags = ViewModel?.Message.Select(GetMsgFragControl) ?? Enumerable.Empty<Control>();

            var controls = badges
                           .Append(username)
                           .Append(UsernameColon)
                           .Concat(frags);

            MessagePanel.Children.Clear();
            MessagePanel.Children.AddRange(controls);
        });
        InitializeComponent();
    }

    private static Control GetImageFromUri(Uri uri)
    {
        return new CachedImage
        {
            Stretch = Stretch.None,
            Margin = new(1, 0),
            Source = uri
        };
    }

    private static Control GetMsgFragControl(IMessageFragment frag)
    {
        return frag switch
        {
            EmoteFragment emoteFragment => GetImageFromUri(emoteFragment.Emote.Uri),
            TextFragment textFragment => new TextBlock
            {
                Text = textFragment.Text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            },
            UriFragment uriFragment => new HyperlinkButton
            {
                Content = uriFragment.Text,
                NavigateUri = uriFragment.Uri,
                Padding = new(5, 5, 5, 6)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(frag))
        };
    }
}