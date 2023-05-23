using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ChatModApp.Tools;

namespace ChatModApp.Controls
{
    public class AdvancedImage : ContentControl
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<Uri?> SourceProperty =
            AvaloniaProperty.Register<AdvancedImage, Uri?>(nameof(Source));

        /// <summary>
        /// Defines the <see cref="IsLoading"/> property.
        /// </summary>
        public static readonly DirectProperty<AdvancedImage, bool> IsLoadingProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImage, bool>(
                nameof(IsLoading),
                image => image._isLoading);

        /// <summary>
        /// Defines the <see cref="CurrentImage"/> property.
        /// </summary>
        public static readonly DirectProperty<AdvancedImage, IImage?> CurrentImageProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImage, IImage?>(
                nameof(CurrentImage),
                image => image._currentImage);

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


        /// <summary>
        /// Gets or sets the URI for image that will be displayed.
        /// </summary>
        public Uri? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets a value indicating is image currently is loading state.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetAndRaise(IsLoadingProperty, ref _isLoading, value);
        }

        /// <summary>
        /// Gets a currently loaded IImage.
        /// </summary>
        public IImage? CurrentImage
        {
            get => _currentImage;
            private set => SetAndRaise(CurrentImageProperty, ref _currentImage, value);
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

        private CancellationTokenSource? _updateCancellationToken;
        private RoundedRect _cornerRadiusClip;
        private bool _isCornerRadiusUsed;
        private readonly Uri? _baseUri;
        private bool _isLoading;
        private IImage? _currentImage;
        private bool _isAnimated = false;

        static AdvancedImage()
        {
            AffectsRender<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty,
                CornerRadiusProperty);
            AffectsMeasure<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty);
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
        {
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == SourceProperty)
            {
                UpdateImage(change.GetNewValue<Uri?>());
            }

            else if (change.Property == CornerRadiusProperty)
            {
                UpdateCornerRadius(change.GetNewValue<CornerRadius>());
            }
            else if (change.Property == BoundsProperty && CornerRadius != default)
            {
                UpdateCornerRadius(CornerRadius);
            }

            base.OnPropertyChanged(change);
        }


        private async void UpdateImage(Uri? source)
        {
            _updateCancellationToken?.Cancel();
            _updateCancellationToken?.Dispose();
            var cancellationTokenSource = _updateCancellationToken = new();
            IsLoading = true;
            CurrentImage = null;

            IBitmap? bitmap = null;
            if (source?.Scheme is "avares")
            {
                // Hack to support relative URI
                var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();
                try
                {
                    if (assetLoader?.Exists(source, _baseUri) ?? false)
                    {
                        bitmap = new Bitmap(assetLoader.Open(source, _baseUri));
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Control)
                        ?.Log(this, "Error while loading bitmap from asset loader: {Exception}", ex);
                }
            }

            try
            {
                bitmap ??= await CachedBitmapStore.Get(source, cancellationTokenSource.Token);

                _isAnimated = bitmap is ImageGifBitmap;
            }
            catch (TaskCanceledException ex)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.Control)
                    ?.Log(this, "Cancelled async bitmap store fetch {Exception}", ex);
            }


            if (cancellationTokenSource.IsCancellationRequested) return;
            CurrentImage = bitmap;
            IsLoading = false;
        }

        private void UpdateCornerRadius(CornerRadius radius)
        {
            _isCornerRadiusUsed = radius != default;
            _cornerRadiusClip = new(new(0, 0, Bounds.Width, Bounds.Height), radius);
        }


        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var source = CurrentImage;

            if (source != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                var viewPort = new Rect(Bounds.Size);
                var sourceSize = source.Size;

                var scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
                var scaledSize = sourceSize * scale;
                var destRect = viewPort
                    .CenterRect(new(scaledSize))
                    .Intersect(viewPort);
                var sourceRect = new Rect(sourceSize)
                    .CenterRect(new(destRect.Size / scale));

                DrawingContext.PushedState? pushedState =
                    _isCornerRadiusUsed ? context.PushClip(_cornerRadiusClip) : null;
                context.DrawImage(source, sourceRect, destRect);
                pushedState?.Dispose();
            }
            else
            {
                base.Render(context);
            }
            
            if(_isAnimated) Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return CurrentImage != null
                ? Stretch.CalculateSize(availableSize, CurrentImage.Size, StretchDirection)
                : base.MeasureOverride(availableSize);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return CurrentImage != null
                ? Stretch.CalculateSize(finalSize, CurrentImage.Size)
                : base.ArrangeOverride(finalSize);
        }
    }
}