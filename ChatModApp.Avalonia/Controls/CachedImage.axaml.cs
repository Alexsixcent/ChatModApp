using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Extensions.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChatModApp.Tools;

namespace ChatModApp.Controls;

public class CachedImage : TemplatedControl, IBitmapSource
{
    public static readonly StyledProperty<Uri?> SourceProperty =
        AvaloniaProperty.Register<CachedImage, Uri?>(nameof(Source));

    public static readonly StyledProperty<IImage?> ImageSourceProperty = Image.SourceProperty.AddOwner<CachedImage>();

    public static readonly StyledProperty<Stretch> StretchProperty = Image.StretchProperty.AddOwner<CachedImage>();

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    private const string ElementImage = "PART_Image";

    private Image? _image;

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

    static CachedImage()
    {
        ImageSourceProperty.Changed
                           .Where(args => args.IsEffectiveValueChange && !args.IsSameValue())
                           .Select(args => ((Image)args.Sender, args.NewValue.Value))
                           .Subscribe(tuple => tuple.Item1.Source = tuple.Value);

        SourceProperty.Changed
                      .ObserveOn(Scheduler.Default)
                      .Where(args => args.IsEffectiveValueChange && !args.IsSameValue())
                      .SelectMany(async (args, _, token) =>
                                      ((IBitmapSource)args.Sender,
                                       await CachedBitmapStore.Get(args.NewValue.Value, token)))
                      .ObserveOn(AvaloniaScheduler.Instance)
                      .Subscribe(tuple => tuple.Item1.SetImageSource(tuple.Item2));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _image = e.NameScope.Find<Image>(ElementImage);
    }

    public void SetImageSource(IBitmap? bitmap)
    {
        if (bitmap is null) return;
        if (_image is null) return;

        _image.Source = bitmap;
    }
}