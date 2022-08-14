using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using ChatModApp.Shared.Models.Chat.Emotes;

namespace ChatModApp.Controls;

[TemplatePart(ToolTipImage, typeof(CachedImage))]
[TemplatePart(EmoteImage, typeof(CachedImage))]
[TemplatePart(EmoteCode, typeof(TextBlock))]
public class ChatEmote : TemplatedControl
{
    private const string ToolTipImage = "PART_ToolTipImage",
                         EmoteImage = "PART_EmoteImage",
                         EmoteCode = "PART_EmoteCode";
    
    public static readonly StyledProperty<IEmote?> EmoteProperty = AvaloniaProperty.Register<ChatEmote, IEmote?>(
     nameof(Emote));

    public IEmote? Emote
    {
        get => GetValue(EmoteProperty);
        set => SetValue(EmoteProperty, value);
    }

    private CachedImage? _toolTipImage, _emoteImage;
    private TextBlock? _emoteCode;

    static ChatEmote()
    {
        EmoteProperty.Changed.AddClassHandler<ChatEmote>(OnEmoteChanged);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _toolTipImage = e.NameScope.Find<CachedImage>(ToolTipImage);
        _emoteImage = e.NameScope.Find<CachedImage>(EmoteImage);
        _emoteCode = e.NameScope.Find<TextBlock>(EmoteCode);
        
        base.OnApplyTemplate(e);
        
        if (Emote is not null)
            OnEmoteChanged(this, new AvaloniaPropertyChangedEventArgs<IEmote?>(this, EmoteProperty, Optional<IEmote?>.Empty, new(Emote),BindingPriority.Unset ));
    }

    //TODO : Replace with template bindings when https://github.com/AvaloniaUI/Avalonia/issues/3823 is fixed
    private static void OnEmoteChanged(ChatEmote arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        if (arg2.NewValue is not IEmote emote)
            return;

        if (arg1._toolTipImage is not null)
        {
            arg1._toolTipImage.Source = emote.Large;
        }

        if (arg1._emoteImage is not null)
        {
            arg1._emoteImage.Source = emote.Small;
        }

        if (arg1._emoteCode is not null)
        {
            arg1._emoteCode.Text = $"Code: {emote.Code}";
        }
    }
}