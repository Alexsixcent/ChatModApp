using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Tools;
using static ChatModApp.Controls.LoadingContentControl;

namespace ChatModApp.Controls;

[TemplatePart(ElementImageControl, typeof(Image))]
public class AdvancedImage : TemplatedControl
{
    public static readonly StyledProperty<LoadingState> StateProperty =
        LoadingContentControl.StateProperty.AddOwner<AdvancedImage>();
    
    public LoadingState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
    
    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<Uri?> SourceProperty =
        AvaloniaProperty.Register<AdvancedImage, Uri?>(nameof(Source));

    
    /// <summary>
    /// Defines the <see cref="CurrentImage"/> property.
    /// </summary>
    public static readonly StyledProperty<IImage?> CurrentImageProperty = Image.SourceProperty.AddOwner<AdvancedImage>();

    /// <summary>
    /// Defines the <see cref="Stretch"/> property.
    /// </summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        Image.StretchProperty.AddOwner<AdvancedImage>();

    /// <summary>
    /// Defines the <see cref="StretchDirection"/> property.
    /// </summary>
    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        Image.StretchDirectionProperty.AddOwner<AdvancedImage>();

    public static readonly StyledProperty<double> PlaceholderWidthProperty = AvaloniaProperty.Register<AdvancedImage, double>(
     "PlaceholderWidth");

    public double PlaceholderWidth
    {
        get => GetValue(PlaceholderWidthProperty);
        set => SetValue(PlaceholderWidthProperty, value);
    }

    public static readonly StyledProperty<double> PlaceholderHeightProperty = AvaloniaProperty.Register<AdvancedImage, double>(
     "PlaceholderHeight");

    public double PlaceholderHeight
    {
        get => GetValue(PlaceholderHeightProperty);
        set => SetValue(PlaceholderHeightProperty, value);
    }



    /// <summary>
    /// Gets or sets the URI for image that will be displayed.
    /// </summary>
    public Uri? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Gets a currently loaded IImage.
    /// </summary>
    public IImage? CurrentImage
    {
        get => GetValue(CurrentImageProperty);
        private set => SetValue(CurrentImageProperty, value);
    }

    /// <summary>
    /// Gets or sets a value controlling how the image will be stretched.
    /// </summary>
    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets a value controlling in what direction the image will be stretched.
    /// </summary>
    public StretchDirection StretchDirection
    {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    private const string ElementImageControl = "PART_Image";

    private readonly Uri? _baseUri;

    private Image? _imagePart;
    private static Task? _updateTask;

    static AdvancedImage()
    {
        SourceProperty.Changed
                      .ObserveOnThreadPool()
                      .SelectMany(async (args, token) =>
                      {
                          var @this = (AdvancedImage)args.Sender;

                          if (!_updateTask?.IsCompleted ?? false)
                          {
                              await _updateTask;
                          }
                          
                          _updateTask = @this.UpdateImage(args.GetNewValue<Uri?>(), token);

                          return Unit.Default;
                      }).Subscribe();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedImage"/> class.
    /// </summary>
    /// <param name="baseUri">The base URL for the XAML context.</param>
    public AdvancedImage(Uri? baseUri)
    {
        _baseUri = baseUri;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedImage"/> class.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider.</param>
    public AdvancedImage(IServiceProvider serviceProvider)
        : this((serviceProvider.GetService(typeof(IUriContext)) as IUriContext)?.BaseUri)
    { }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _imagePart = e.NameScope.Get<Image>(ElementImageControl);

        if (CurrentImage is AnimatedBitmap sharpBitmap)
        {
            sharpBitmap.InvalidateVisual += (_, _) => _imagePart.InvalidateVisual();
            sharpBitmap.Start();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurrentImageProperty)
        {
            if (change.NewValue is AnimatedBitmap newBitmap)
            {
                newBitmap.IterationCount = IterationCount.Infinite;
                newBitmap.InvalidateVisual += (_, _) => _imagePart?.InvalidateVisual();
                newBitmap.Start();
            }
        }
    }


    private async Task UpdateImage(Uri? source, CancellationToken token = default)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            State = LoadingState.Loading;
            CurrentImage = null;
        }, DispatcherPriority.Default, token);

        IImage? bitmap = null;
        try
        {
            bitmap ??= await CachedBitmapStore.Get(source, _baseUri, token);
        }
        catch (TaskCanceledException ex)
        {
            Logger.TryGet(LogEventLevel.Debug, LogArea.Control)
                  ?.Log(this, "Cancelled async bitmap store fetch {Exception}", ex);
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                  ?.Log(this, "Could not load bitmap from URI {URI}, {Exception}", source, ex);
            await Dispatcher.UIThread.InvokeAsync(() => State = LoadingState.Failed, DispatcherPriority.Default, token);
        }

        if (token.IsCancellationRequested)
        {
            await Dispatcher.UIThread.InvokeAsync(() => State = LoadingState.Failed, DispatcherPriority.Default, token);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentImage = bitmap;
            State = LoadingState.Loaded;
        }, DispatcherPriority.Default, token);
    }
}