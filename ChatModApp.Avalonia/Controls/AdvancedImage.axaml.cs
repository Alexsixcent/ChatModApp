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
using AvaloniaGif;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Tools;

namespace ChatModApp.Controls;

[TemplatePart(ElementImageControl, typeof(Image))]
[TemplatePart(ElementPlaceholderControl, typeof(ContentControl))]
public class AdvancedImage : TemplatedControl
{
    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<Uri?> SourceProperty =
        AvaloniaProperty.Register<AdvancedImage, Uri?>(nameof(Source));

    public static readonly StyledProperty<object?> PlaceholderContentProperty =
        AvaloniaProperty.Register<AdvancedImage, object?>(nameof(PlaceholderContent));

    public static readonly StyledProperty<ImageState> StateProperty =
        AvaloniaProperty.Register<AdvancedImage, ImageState>(nameof(State));

    public static readonly StyledProperty<IPageTransition?> ImageTransitionProperty =
        AvaloniaProperty.Register<AdvancedImage, IPageTransition?>(nameof(ImageTransition));

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

    public ImageState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public object? PlaceholderContent
    {
        get => GetValue(PlaceholderContentProperty);
        set => SetValue(PlaceholderContentProperty, value);
    }

    public IPageTransition? ImageTransition
    {
        get => GetValue(ImageTransitionProperty);
        set => SetValue(ImageTransitionProperty, value);
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
    private const string ElementPlaceholderControl = "PART_Placeholder";
    
    private readonly Uri? _baseUri;

    private Image? _imagePart;
    private ContentControl? _placeholderPart;
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

        StateProperty.Changed
                     .SelectMany(async (args, token) =>
                     {
                         async Task Transition(Visual? start, Visual? end, IPageTransition? transition, CancellationToken cancellationToken = default)
                         {
                             if (transition is null)
                             {
                                 if (start is not null) start.IsVisible = false;
                                 if (end is not null) end.IsVisible = true;
                             }
                             else
                                 await transition.Start(start, end, true, cancellationToken);
                         }
                         
                         var img = (AdvancedImage)args.Sender;
                         
                         if (args.IsSameValue()) return Unit.Default;
                         
                         var oldState = args.GetOldValue<ImageState>();
                         var newState = args.GetNewValue<ImageState>();

                         switch (oldState)
                         {
                             case ImageState.Unloaded when newState is ImageState.Loading:
                                 await Transition(null, img._placeholderPart, img.ImageTransition, token);
                                 break;
                             case ImageState.Loading when newState is ImageState.Loaded:
                                 await Transition(img._placeholderPart, img._imagePart, img.ImageTransition, token);
                                 break;
                             case ImageState.Loaded when newState is ImageState.Loading:
                                 await Transition(img._imagePart, img._placeholderPart, img.ImageTransition, token);
                                 break;
                             case ImageState.Loaded when newState is ImageState.Unloaded:
                                 await Transition(img._imagePart, null, img.ImageTransition, token);
                                 break;
                             case ImageState.Failed:
                                 Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(img, "[AdvancedImage] Failed loading image at {URI}", img.Source);
                                 break;
                             default:
                                 Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(img, "[AdvancedImage] Unknown state transition from {OldState} to {NewState}", oldState, newState);
                                 break;
                         }

                         return Unit.Default;
                     })
                     .Subscribe();
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
        _placeholderPart = e.NameScope.Get<ContentControl>(ElementPlaceholderControl);

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
            State = ImageState.Loading;
            CurrentImage = null;
        }, DispatcherPriority.Layout, token);

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
            await Dispatcher.UIThread.InvokeAsync(() => State = ImageState.Failed, DispatcherPriority.Layout, token);
        }

        if (token.IsCancellationRequested)
        {
            await Dispatcher.UIThread.InvokeAsync(() => State = ImageState.Failed, DispatcherPriority.Layout, token);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentImage = bitmap;
            State = ImageState.Loaded;
        }, DispatcherPriority.Layout, token);
    }


    public enum ImageState
    {
        Unloaded,
        Loading,
        Loaded,
        Failed
    }
}