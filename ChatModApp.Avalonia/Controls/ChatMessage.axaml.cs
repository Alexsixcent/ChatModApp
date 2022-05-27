using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Extensions.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Models.Chat.Fragments;

namespace ChatModApp.Controls;

//TODO: Rework with inline text and UI when it comes in 0.11
[TemplatePart(ElementPanel, typeof(WrapPanel))]
[TemplatePart(ElementBadgesPanel, typeof(StackPanel))]
[TemplatePart(ElementUserBlock, typeof(TextBlock))]
public class ChatMessage : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable<IChatBadge>> BadgesProperty =
        AvaloniaProperty.Register<ChatMessage, IEnumerable<IChatBadge>>(nameof(Badges),
                                                                        defaultBindingMode: BindingMode.OneTime);

    public static readonly StyledProperty<IEnumerable<IMessageFragment>> MessageFragmentsProperty =
        AvaloniaProperty.Register<ChatMessage, IEnumerable<IMessageFragment>>(nameof(MessageFragments),
                                                                              defaultBindingMode: BindingMode.OneTime);

    public static readonly DirectProperty<ChatMessage, string> UsernameProperty =
        TextBlock.TextProperty.AddOwner<ChatMessage>(m => m.Username, (m, s) => m.Username = s, string.Empty,
                                                     BindingMode.OneTime);

    public static readonly StyledProperty<Color> UsernameColorProperty =
        SolidColorBrush.ColorProperty.AddOwner<ChatMessage>();

    public IEnumerable<IChatBadge> Badges
    {
        get => GetValue(BadgesProperty);
        set => SetValue(BadgesProperty, value);
    }

    public IEnumerable<IMessageFragment> MessageFragments
    {
        get => GetValue(MessageFragmentsProperty);
        set => SetValue(MessageFragmentsProperty, value);
    }

    public string Username
    {
        get => _username;
        set => SetAndRaise(UsernameProperty, ref _username, value);
    }

    public Color UsernameColor
    {
        get => GetValue(UsernameColorProperty);
        set => SetValue(UsernameColorProperty, value);
    }

    private const string ElementPanel = "PART_Panel",
                         ElementBadgesPanel = "PART_BadgesPanel",
                         ElementUserBlock = "PART_UserBlock";

    private string _username = string.Empty;

    private WrapPanel? _panel;
    private StackPanel? _badgesPanel;
    private TextBlock? _usernameBlock;

    static ChatMessage()
    {
        BadgesProperty.Changed.AddClassHandler<ChatMessage>((message, args) => message.OnBadgesChanged(args));
        MessageFragmentsProperty.Changed.AddClassHandler<ChatMessage>((message, args) =>
                                                                          message.OnFragmentsChanged(args));
    }

    private void OnBadgesChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is IEnumerable<IChatBadge> badges)
        {
            BuildBadges(badges);
        }
    }

    private void OnFragmentsChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is IEnumerable<IMessageFragment> frags)
        {
            RebuildFragments(frags);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _panel = e.NameScope.Find<WrapPanel>(ElementPanel);
        _badgesPanel = e.NameScope.Find<StackPanel>(ElementBadgesPanel);
        _usernameBlock = e.NameScope.Find<TextBlock>(ElementUserBlock);

        BuildBadges(Badges);
        RebuildFragments(MessageFragments);

        base.OnApplyTemplate(e);
    }

    private void BuildBadges(IEnumerable<IChatBadge> badges)
    {
        _badgesPanel?.Children.Clear();
        _badgesPanel?.Children.AddRange(badges.Select(badge => GetImageFromUri(badge.Small)));
    }

    private void RebuildFragments(IEnumerable<IMessageFragment> fragments)
    {
        if (_panel?.Children.Count > 2)
        {
            _panel?.Children.RemoveRange(2, _panel.Children.Count - 2);
        }
        
        _panel?.Children.AddRange(fragments.Select(GetMsgFragControl));
    }

    private static Control GetImageFromUri(Uri uri)
    {
        return new CachedImage
        {
            Source = uri,
            Stretch = Stretch.None,
            Margin = new(1, 0)
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