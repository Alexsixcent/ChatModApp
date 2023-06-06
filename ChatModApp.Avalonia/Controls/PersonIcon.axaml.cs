using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Tools;

namespace ChatModApp.Controls;

public class PersonIcon : TemplatedControl
{
    public static readonly StyledProperty<Uri?> SourceProperty = AvaloniaProperty.Register<PersonIcon, Uri?>(
     nameof(Source));

    public static readonly StyledProperty<IImage?> ImageSourceProperty =
        AvaloniaProperty.Register<PersonIcon, IImage?>(nameof(ImageSource));

    public static readonly StyledProperty<IBrush?> BorderStrokeProperty =
        Shape.StrokeProperty.AddOwner<PersonIcon>();

    public static readonly StyledProperty<double> BorderStrokeThicknessProperty =
        Shape.StrokeThicknessProperty.AddOwner<PersonIcon>();

    public Uri? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IImage? ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public IBrush? BorderStroke
    {
        get => GetValue(BorderStrokeProperty);
        set => SetValue(BorderStrokeProperty, value);
    }

    public double BorderStrokeThickness
    {
        get => GetValue(BorderStrokeThicknessProperty);
        set => SetValue(BorderStrokeThicknessProperty, value);
    }

    private Ellipse? _ellipse;

    static PersonIcon()
    {
        var img = ImageSourceProperty.Changed
                                     .Where(args => !args.IsSameValue())
                                     .Select(args => ((PersonIcon)args.Sender, args.NewValue.Value));
        SourceProperty.Changed
                      .ObserveOnThreadPool()
                      .Where(args => !args.IsSameValue())
                      .SelectMany(async args =>
                                      ((PersonIcon)args.Sender, await CachedBitmapStore.Get(args.NewValue.Value)))
                      .Merge(img)
                      .ObserveOnMainThread()
                      .Subscribe(tuple => tuple.Item1.SetImageSource(tuple.Item2));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _ellipse = e.NameScope.Find<Ellipse>("PART_Ellipse");
    }

    public void SetImageSource(IImage? bitmap)
    {
        if (bitmap is null) return;
        if (_ellipse is null) return;

        try
        {
            if (_ellipse.Fill is ImageBrush brush)
            {
                switch (bitmap)
                {
                    case AnimatedBitmap sharpBitmap:
                        sharpBitmap.InvalidateVisual += (_, _) => brush.Source = sharpBitmap.CurrentTarget;
                        sharpBitmap.Start();
                        break;
                    case Bitmap bitmapSource:
                        brush.Source = bitmapSource;
                        break;
                }
            }

            SetSize(_ellipse, bitmap.Size);
            DrawAgain();
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, ex.Message);
        }
    }

    private void DrawAgain()
    {
        _ellipse?.InvalidateVisual();
        _ellipse?.InvalidateMeasure();
    }

    private static void SetSize(Layoutable control, Size size)
    {
        switch (control.Width)
        {
            case double.NaN when double.IsNaN(control.Height):
                control.Height = control.Width = Math.Min(size.Width, size.Height);
                break;
            case double.NaN when control.Height > 0:
                control.Width = control.Height;
                break;
            case > 0 when double.IsNaN(control.Height):
                control.Height = control.Width;
                break;
        }
    }
}