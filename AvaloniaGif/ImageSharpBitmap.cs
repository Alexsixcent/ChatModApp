#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Size = Avalonia.Size;

namespace AvaloniaGif;

public sealed class ImageSharpBitmap : IImage, IDisposable
{
    public event EventHandler? InvalidateVisual;
    public Size Size => PixelSize.ToSizeWithDpi(Dpi);
    public PixelSize PixelSize => new(_sourceImage?.Width ?? 0, _sourceImage?.Height ?? 0);
    public Vector Dpi { get; private set; }
    public IterationCount IterationCount { get; set; }
    public RenderTargetBitmap? CurrentTarget => _currentFrameIndex < 0 || _currentFrameIndex >= _targetFrames.Count ? 
                                                    null : _targetFrames[_currentFrameIndex];

    public bool IsAnimated => _targetFrames.Count > 1;
    public CancellationTokenSource CurrentCts { get; }

    private readonly object _sync;
    private readonly Stopwatch _watch;
    private readonly List<TimeSpan> _frameDelays;
    private readonly List<RenderTargetBitmap> _targetFrames;

    private Image? _sourceImage;
    private TimeSpan _totalTime;
    private uint _iterationCount;
    private int _currentFrameIndex;
    private double _averageFrameTime;

    private bool _isRunning;
    private DispatcherTimer? _timer;


    public ImageSharpBitmap()
    {
        CurrentCts = new();
        _sync = new();
        _watch = new();
        _targetFrames = new();
        _frameDelays = new();
        _totalTime = TimeSpan.Zero;
        _currentFrameIndex = -1;
    }

    public async Task Load(string str, CancellationToken token = default)
    {
        await Load(GetStreamFromString(str), token);
    }

    public async Task Load(Uri uri, CancellationToken token = default)
    {
        await Load(GetStreamFromUri(uri), token);
    }

    public async Task<bool> Load(Stream stream, CancellationToken token = default)
    {
        var options = new DecoderOptions
        {
            Configuration = Configuration.Default
        };
        options.Configuration.ReadOrigin = ReadOrigin.Begin;

        Image<Rgba32> image;
        try
        {
            image = await Image.LoadAsync<Rgba32>(options, stream, token);
        }
        catch (NotSupportedException e)
        {
            return false;
        }
        catch (InvalidImageContentException e)
        {
            return false;
        }
        catch (UnknownImageFormatException e)
        {
            return false;
        }

        var alphaType = image.PixelType.AlphaRepresentation switch
        {
            PixelAlphaRepresentation.None => AlphaFormat.Opaque,
            PixelAlphaRepresentation.Associated => AlphaFormat.Premul,
            PixelAlphaRepresentation.Unassociated => AlphaFormat.Unpremul,
            _ => AlphaFormat.Unpremul
        };



        var dpi = new Vector(96,96);

        foreach (var frame in image.Frames)
        {
            var frameDelay = TimeSpan.Zero;
            if (frame.Metadata.TryGetGifMetadata(out var metadataGif))
            {
                frameDelay = TimeSpan.FromMilliseconds(metadataGif.FrameDelay * 10);
            }
            else if (frame.Metadata.TryGetFormatMetadata(WebpFormat.Instance, out var metadataWebp))
            {
                frameDelay = TimeSpan.FromMilliseconds(metadataWebp?.FrameDuration ?? 0);
            }

            _frameDelays.Add(frameDelay);

            var target = CopyToBitmap(frame, alphaType, dpi);

            _targetFrames.Add(target);
        }
        
        Dpi = dpi;
        _sourceImage = image;
        _totalTime = _frameDelays.Aggregate((t1, t2) => t1 + t2);
        _averageFrameTime = _frameDelays.Average(span => span.TotalSeconds);
        return true;
    }

    private unsafe RenderTargetBitmap CopyToBitmap(ImageFrame<Rgba32> frame, AlphaFormat alphaType, Vector dpi)
    {
        var pixelSize = new PixelSize(frame.Width, frame.Height);
        using WriteableBitmap bitmap = new(pixelSize, dpi, PixelFormat.Rgba8888, alphaType);
        using var handle = bitmap.Lock();

        var span = new Span<byte>((void*)handle.Address, handle.Size.Height * handle.RowBytes);

        frame.CopyPixelDataTo(span);

        var renderTarget = new RenderTargetBitmap(pixelSize, dpi);
        
        using var ctx = renderTarget.CreateDrawingContext();
        ctx.DrawImage(bitmap, new(0,0,pixelSize.Width, pixelSize.Height));
        
        return renderTarget;
    }

    public void Start()
    {
        if (_isRunning)
            return;
        
        _iterationCount = 0;

        if (!IsAnimated)
        {
            _currentFrameIndex = 0;
            InvalidateVisual?.Invoke(this, EventArgs.Empty);
            return;
        }

        const double maxFrameTime = 1 / 60.0;
        _timer = new(TimeSpan.FromSeconds(Math.Max(maxFrameTime, _averageFrameTime)), DispatcherPriority.Render, Tick);
        _timer.Start();

        _watch.Start();
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;

        _timer?.Stop();
        _timer = null;

        _watch.Reset();

        _iterationCount = 0;
    }

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        if (CurrentCts.IsCancellationRequested)
            return;

        lock (_sync)
        {
            if (CurrentTarget is null) return;

            var bitmap = CurrentTarget;
            if (!_isRunning || bitmap is null)
                bitmap = _targetFrames.First();

            context.DrawImage(bitmap, sourceRect, destRect);
        }
    }

    public void Dispose()
    {
        CurrentCts.Cancel();

        lock (_sync)
        {
            Stop();

            foreach (var frame in _targetFrames)
                frame.Dispose();

            _sourceImage?.Dispose();
        }
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (_timer is null)
            return;

        if (!IterationCount.IsInfinite)
            if (IterationCount.Value == 0 || _iterationCount >= IterationCount.Value)
            {
                _isRunning = false;
                _timer.Stop();
                _watch.Stop();
                InvalidateVisual?.Invoke(this, EventArgs.Empty);
            }

        if (_isRunning)
        {
            ProcessFrameTime();
            InvalidateVisual?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _timer.Stop();
        }
    }

    private static Stream GetStreamFromString(string? str)
    {
        if (!Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out var res))
        {
            throw new InvalidCastException("The string provided can't be converted to URI.");
        }

        return GetStreamFromUri(res);
    }

    private static Stream GetStreamFromUri(Uri uri)
    {
        var uriString = uri.OriginalString.Trim();

        if (!uriString.StartsWith("resm") && !uriString.StartsWith("avares"))
            throw new InvalidDataException("The URI provided is not currently supported.");

        return AssetLoader.Open(uri);
    }

    private RenderTargetBitmap? ProcessFrameTime()
    {
        if (!IterationCount.IsInfinite && _iterationCount > IterationCount.Value)
            return null;

        if (CurrentCts.IsCancellationRequested)
            return null;

        if (_timer is null)
            return null;

        var frameTime = _watch.Elapsed;

        if (frameTime > _totalTime)
        {
            _watch.Restart();
            _iterationCount++;
        }

        var timeModulus = TimeSpan.FromTicks(frameTime.Ticks % _totalTime.Ticks);

        var timeCursor = TimeSpan.Zero;
        var currentFrame = 0;
        for (var i = 0; i < _frameDelays.Count; i++)
        {
            var frameDelay = _frameDelays[i];

            if (timeCursor + frameDelay >= timeModulus)
            {
                currentFrame = i;
                break;
            }

            timeCursor += frameDelay;
        }

        _currentFrameIndex = currentFrame;

        return CurrentTarget;
    }
}