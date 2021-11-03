using System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using ChatModApp.Models.Chat.Fragments;

namespace ChatModApp.Views.Controls.MessageView;

[TemplatePart(Name = PartRichTextBlock, Type = typeof(RichTextBlock))]
public partial class MessageView : Control
{
    private const string PartRichTextBlock = nameof(RichTextBlock);
    private const int InlineImagesVerticalOffset = 2;
    private readonly Span _badges;
    private readonly Span _message;
    private readonly Run _separator = new() { Text = ": " };
    private readonly Run _username;
    private RichTextBlock? _richTextBlock;

    public MessageView()
    {
        DefaultStyleKey = typeof(MessageView);

        _badges = new();
        _username = new()
        {
            Text = "placeholder",
            FontWeight = FontWeights.Bold
        };
        _message = new();

        RegisterPropertyChangedCallback(MessageFragmentsProperty, OnMessageChanged);
        RegisterPropertyChangedCallback(ChatBadgesProperty, OnBadgesChanged);
        RegisterPropertyChangedCallback(UsernameProperty, OnUsernameChanged);
        RegisterPropertyChangedCallback(UsernameColorProperty, OnUserColorChanged);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _richTextBlock = (RichTextBlock)GetTemplateChild(PartRichTextBlock);

        _richTextBlock.Blocks.Add(new Paragraph
        {
            Inlines = { _badges, _username, _separator, _message }
        });
    }

    protected static Inline CreateFragmentInline(IMessageFragment fragment)
    {
        return fragment switch
        {
            TextFragment textFrag => new Run { Text = textFrag.Text },
            EmoteFragment emoteFrag => new InlineUIContainer
            {
                Child = new Image
                {
                    Source = new BitmapImage(emoteFrag.Emote.Uri),
                    Stretch = Stretch.None,
                    Margin = new(2, 0, 2, 0),
                    RenderTransform = new TranslateTransform { Y = InlineImagesVerticalOffset }
                }
            },
            UriFragment uriFrag => new Hyperlink
            {
                Inlines = { new Run { Text = uriFrag.Text } },
                NavigateUri = uriFrag.Uri,
                UnderlineStyle = UnderlineStyle.None
            },
            _ => throw new ArgumentException("Fragment not of valid type.")
        };
    }

    private void OnMessageChanged(DependencyObject sender, DependencyProperty dp)
    {
        _message.Inlines.Clear();
        foreach (var frag in MessageFragments) _message.Inlines.Add(CreateFragmentInline(frag));
    }

    private void OnBadgesChanged(DependencyObject sender, DependencyProperty dp)
    {
        _badges.Inlines.Clear();
        foreach (var badge in ChatBadges)
            _badges.Inlines.Add(new InlineUIContainer
            {
                Child = new Image
                {
                    Source = new BitmapImage(badge.Small),
                    Stretch = Stretch.None,
                    Margin = new(2, 0, 2, 0),
                    RenderTransform = new TranslateTransform { Y = InlineImagesVerticalOffset }
                }
            });
    }

    private void OnUsernameChanged(DependencyObject sender, DependencyProperty dp)
    {
        _username.Text = Username;
    }

    private void OnUserColorChanged(DependencyObject sender, DependencyProperty dp)
    {
        _username.Foreground = new SolidColorBrush(UsernameColor);
    }
}