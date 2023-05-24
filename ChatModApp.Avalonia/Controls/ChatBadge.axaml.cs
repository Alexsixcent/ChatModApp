using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using ChatModApp.Shared.Models.Chat;

namespace ChatModApp.Controls;

[TemplatePart(ToolTipImage, typeof(AdvancedImage))]
[TemplatePart(ToolTipText, typeof(TextBlock))]
[TemplatePart(BadgeImage, typeof(AdvancedImage))]
public class ChatBadge : TemplatedControl
{
    private const string ToolTipImage = "PART_ToolTipImage",
                         ToolTipText = "PART_ToolTipText",
                         BadgeImage = "PART_BadgeImage";
    
    public static readonly StyledProperty<IChatBadge?> BadgeProperty = AvaloniaProperty.Register<ChatBadge, IChatBadge?>(
     nameof(Badge));

    public IChatBadge? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    private AdvancedImage? _toolTipImage, _badgeImage;
    private TextBlock? _toolTipTextBlock;

    static ChatBadge()
    {
        BadgeProperty.Changed.AddClassHandler<ChatBadge>(OnEmoteChanged);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _toolTipImage = e.NameScope.Find<AdvancedImage>(ToolTipImage);
        _badgeImage = e.NameScope.Find<AdvancedImage>(BadgeImage);
        _toolTipTextBlock = e.NameScope.Find<TextBlock>(ToolTipText);
        
        base.OnApplyTemplate(e);
        
        if (Badge is not null)
            OnEmoteChanged(this, new AvaloniaPropertyChangedEventArgs<IChatBadge?>(this, BadgeProperty, Optional<IChatBadge?>.Empty, new(Badge),BindingPriority.Unset ));
    }

    //TODO : Replace with template bindings when https://github.com/AvaloniaUI/Avalonia/issues/3823 is fixed
    private static void OnEmoteChanged(ChatBadge control, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.NewValue is not IChatBadge badge)
            return;

        if (control._toolTipImage is not null) 
            control._toolTipImage.Source = badge.Large;

        if (control._badgeImage is not null) 
            control._badgeImage.Source = badge.Small;

        if (control._toolTipTextBlock is not null) 
            control._toolTipTextBlock.Text = badge.Description;
    }
}