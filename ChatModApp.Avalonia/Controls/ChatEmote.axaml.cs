using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using ChatModApp.Shared.Models.Chat.Emotes;

namespace ChatModApp.Controls;

[TemplatePart(ToolTipImage, typeof(CachedImage))]
[TemplatePart(ToolTipText, typeof(TextBlock))]
[TemplatePart(EmoteImage, typeof(CachedImage))]
public class ChatEmote : TemplatedControl
{
    private const string ToolTipImage = "PART_ToolTipImage",
                         ToolTipText = "PART_ToolTipText",
                         EmoteImage = "PART_EmoteImage";
    
    public static readonly StyledProperty<IEmote?> EmoteProperty = AvaloniaProperty.Register<ChatEmote, IEmote?>(
     nameof(Emote));

    public IEmote? Emote
    {
        get => GetValue(EmoteProperty);
        set => SetValue(EmoteProperty, value);
    }

    private CachedImage? _toolTipImage, _emoteImage;
    private TextBlock? _toolTipTextBlock;

    static ChatEmote()
    {
        EmoteProperty.Changed.AddClassHandler<ChatEmote>(OnEmoteChanged);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _toolTipImage = e.NameScope.Find<CachedImage>(ToolTipImage);
        _emoteImage = e.NameScope.Find<CachedImage>(EmoteImage);
        _toolTipTextBlock = e.NameScope.Find<TextBlock>(ToolTipText);
        
        base.OnApplyTemplate(e);
        
        if (Emote is not null)
            OnEmoteChanged(this, new AvaloniaPropertyChangedEventArgs<IEmote?>(this, EmoteProperty, Optional<IEmote?>.Empty, new(Emote),BindingPriority.Unset ));
    }

    //TODO : Replace with template bindings when https://github.com/AvaloniaUI/Avalonia/issues/3823 is fixed
    private static void OnEmoteChanged(ChatEmote control, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.NewValue is not IEmote emote)
            return;

        if (control._toolTipImage is not null)
        {
            control._toolTipImage.Source = emote.Large;
        }

        if (control._emoteImage is not null)
        {
            control._emoteImage.Source = emote.Small;
        }

        if (control._toolTipTextBlock is not null)
        {
            control._toolTipTextBlock.Text = $"Code: {emote.Code}\nProvider: {emote.Provider}\n{emote.Description}";
        }
    }
}