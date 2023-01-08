using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Models.Chat.Fragments;
using FluentAvalonia.UI.Controls;

namespace ChatModApp.Controls;

[TemplatePart(ElementMessageBlock, typeof(TextBlock))]
[TemplatePart(ElementBadgeInlines, typeof(Span))]
[TemplatePart(ElementFragInlines, typeof(Span))]
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

    private const string ElementMessageBlock = "PART_MessageBlock",
        ElementBadgeInlines = "PART_BadgeInlines",
        ElementFragInlines = "PART_FragInlines";

    private string _username = string.Empty;

    private TextBlock? _messageBlock;
    private Span? _badgeInlines, _fragInlines;

    static ChatMessage()
    {
        BadgesProperty.Changed.AddClassHandler<ChatMessage>((message, args) => message.OnBadgesChanged(args));
        MessageFragmentsProperty.Changed.AddClassHandler<ChatMessage>((message, args) =>
            message.OnFragmentsChanged(args));
    }

    private void OnBadgesChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not IEnumerable<IChatBadge> badges) return;

        SetBadges(badges);
    }

    private void OnFragmentsChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not IEnumerable<IMessageFragment> fragments) return;

        SetFragments(fragments);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _messageBlock = e.NameScope.Find<TextBlock>(ElementMessageBlock);
        _badgeInlines = e.NameScope.Find<Span>(ElementBadgeInlines);
        _fragInlines = e.NameScope.Find<Span>(ElementFragInlines);

        base.OnApplyTemplate(e);

        SetBadges(Badges);
        SetFragments(MessageFragments);
    }

    private void SetBadges(IEnumerable<IChatBadge> badges)
    {
        _badgeInlines?.Inlines.Clear();
        _badgeInlines?.Inlines.AddRange(badges.Select(GetInlineFromBadge));
    }

    private void SetFragments(IEnumerable<IMessageFragment> fragments)
    {
        _fragInlines?.Inlines.Clear();
        _fragInlines?.Inlines.AddRange(fragments.Select(GetInlineFromFragment));
    }

    private static Inline GetInlineFromBadge(IChatBadge badge) =>
        new InlineUIContainer(new ChatBadge {Badge = badge})
        {
            BaselineAlignment = BaselineAlignment.Center
        };

    private static Inline GetInlineFromEmote(IEmote emote) =>
        new InlineUIContainer(new ChatEmote {Emote = emote})
        {
            BaselineAlignment = BaselineAlignment.Center
        };

    private static Inline GetInlineFromFragment(IMessageFragment frag)
    {
        return frag switch
        {
            EmoteFragment emoteFragment => GetInlineFromEmote(emoteFragment.Emote),
            TextFragment textFragment => new Run(textFragment.Text)
            {
                FontWeight = FontWeight.Normal,
                BaselineAlignment = BaselineAlignment.Center
            },
            UriFragment uriFragment => new InlineUIContainer(
                new HyperlinkButton
                {
                    Content = uriFragment.Text,
                    NavigateUri = uriFragment.Uri,
                    Padding = new(5, 5, 5, 6)
                })
            {
                BaselineAlignment = BaselineAlignment.Center
            },
            _ => throw new ArgumentOutOfRangeException(nameof(frag))
        };
    }
}