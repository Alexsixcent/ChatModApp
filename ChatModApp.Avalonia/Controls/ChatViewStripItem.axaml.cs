using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Logging;
using Avalonia.Media;

namespace ChatModApp.Controls;

[TemplatePart(ElementContentControl, typeof(ContentControl))]
public class ChatViewStripItem : TemplatedControl
{
    public static readonly StyledProperty<object?> ContentProperty = ContentControl.ContentProperty.AddOwner<ChatViewStripItem>();

    public static readonly StyledProperty<bool> IsStrippedProperty = AvaloniaProperty.Register<ChatViewStripItem, bool>(
     nameof(IsStripped));

    public static readonly StyledProperty<IBrush?> StripColorBrushProperty = AvaloniaProperty.Register<ChatViewStripItem, IBrush?>(
     nameof(StripColorBrush));

    public static readonly StyledProperty<double> StripSizeProperty =
        AvaloniaProperty.Register<ChatViewStripItem, double>(nameof(StripSize), 4);

    public static readonly StyledProperty<double> StripSpanProperty =
        AvaloniaProperty.Register<ChatViewStripItem, double>(nameof(StripSpan), 10);

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public bool IsStripped
    {
        get => GetValue(IsStrippedProperty);
        set => SetValue(IsStrippedProperty, value);
    }

    public IBrush? StripColorBrush
    {
        get => GetValue(StripColorBrushProperty);
        set => SetValue(StripColorBrushProperty, value);
    }

    public double StripSize
    {
        get => GetValue(StripSizeProperty);
        set => SetValue(StripSizeProperty, value);
    }

    public double StripSpan
    {
        get => GetValue(StripSpanProperty);
        set => SetValue(StripSpanProperty, value);
    }


    private const string ElementContentControl = "PART_ContentControl";
    private ContentControl? _contentControl;

    static ChatViewStripItem()
    {
        IsStrippedProperty.Changed.Subscribe(args =>
        {
            if (args.NewValue == args.OldValue) return;

            var @this = (ChatViewStripItem)args.Sender;
            @this.SetStrip(args.NewValue.Value);
        });

        StripSizeProperty.Changed.Subscribe(args =>
        {
            var @this = (ChatViewStripItem)args.Sender;

            if (@this.StripSize <= @this.StripSpan)
            {
                @this.SetStrip(@this.IsStripped);
                return;
            }

            Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                  ?.Log(@this, "Strip item size can't be bigger than strip span!");
            @this.StripSize = args.OldValue.Value;
        });

        StripSpanProperty.Changed.Subscribe(args =>
        {
            var @this = (ChatViewStripItem)args.Sender;

            if (@this.StripSize <= @this.StripSpan)
            {
                @this.SetStrip(@this.IsStripped);
                return;
            }

            Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                  ?.Log(@this, "Strip item span can't be smaller than strip size!");
            @this.StripSpan = args.OldValue.Value;
        });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _contentControl = e.NameScope.Find<ContentControl>(ElementContentControl);

        base.OnApplyTemplate(e);
        
        SetStrip(IsStripped);
    }

    private void SetStrip(bool isVisible)
    {
        if (_contentControl is null) return;

        if (isVisible)
        {
            _contentControl.Margin = new(StripSpan - StripSize, 0, 0, 0);
        }
        else
        {
            _contentControl.Margin = new(StripSpan, 0, 0, 0);
        }
    }
}