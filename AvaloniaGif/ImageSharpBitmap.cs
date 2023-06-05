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
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;
using Size = Avalonia.Size;

namespace AvaloniaGif;

public sealed class ImageSharpBitmap : IImage, IDisposable
{
    public event EventHandler? InvalidateVisual;
    public Size Size => PixelSize.ToSizeWithDpi(Dpi);
    public PixelSize PixelSize { get; private set; }
    public Vector Dpi { get; private set; }
    public IterationCount IterationCount { get; set; }

    public RenderTargetBitmap? CurrentTarget => _currentFrameIndex < 0 || _currentFrameIndex >= _targetFrames.Length
                                                    ? null
                                                    : _targetFrames[_currentFrameIndex];

    public bool IsAnimated => _targetFrames.Length > 1;

    private readonly object _sync;
    private readonly Stopwatch _watch;
    private RenderTargetBitmap[] _targetFrames;
    
    private TimeSpan _totalTime;
    private uint _iterationCount;
    private int _currentFrameIndex;
    private TimeSpan _averageFrameTime;

    private bool _isRunning;
    private DispatcherTimer? _timer;
    private List<TimeSpan> _frameDelays;

    // Capping the framerate at 60fps since that is what Avalonia supports by default
    private const double MaxFrameTime = 1 / 60.0;


    public ImageSharpBitmap()
    {
        _sync = new();
        _watch = new();
        _targetFrames = Array.Empty<RenderTargetBitmap>();
        _frameDelays = new();
        _totalTime = TimeSpan.Zero;
        _currentFrameIndex = -1;
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

        PixelSize = new(image.Width, image.Height);

        var alphaType = image.PixelType.AlphaRepresentation switch
        {
            PixelAlphaRepresentation.None => AlphaFormat.Opaque,
            PixelAlphaRepresentation.Associated => AlphaFormat.Premul,
            PixelAlphaRepresentation.Unassociated => AlphaFormat.Unpremul,
            _ => AlphaFormat.Unpremul
        };

        var dpi = new Vector(96, 96);

        IEnumerable<ImageFrame<Rgba32>> processedFrames;
        switch (image.Metadata.DecodedImageFormat)
        {
            case GifFormat:
                var tempImage = image.Frames.CloneFrame(0);

                for (var i = 0; i < image.Frames.Count; i++)
                {
                    var frame = image.Frames[i];
                    var metadata = frame.Metadata.GetGifMetadata();

                    var frameDelay = TimeSpan.FromSeconds(Math.Max(MaxFrameTime, metadata.FrameDelay / 100.0));
                    _frameDelays.Add(frameDelay);

                    ProcessGifFrames(image.Frames, tempImage.Frames, i, metadata.DisposalMethod);
                }

                processedFrames = tempImage.Frames.AsEnumerable();
                break;
            case WebpFormat:
                _frameDelays = image.Frames.Select(frame =>
                {
                    var metadata = frame.Metadata.GetWebpMetadata();
                    var frameDelay = TimeSpan.FromSeconds(Math.Max(MaxFrameTime, metadata.FrameDuration / 1000.0));
                    return frameDelay;
                }).ToList();

                processedFrames = image.Frames.AsEnumerable();
                break;
            default:
                processedFrames = image.Frames.AsEnumerable();
                break;
        }


        // ReSharper disable once PossibleMultipleEnumeration
        _targetFrames = processedFrames
                        .Select(frame => CopyToBitmap(frame, alphaType, dpi))
                        .ToArray();

        Dpi = dpi;

        if (_frameDelays.Count > 0)
        {
            _totalTime = _frameDelays.Aggregate((t1, t2) => t1 + t2);
            _averageFrameTime = TimeSpan.FromSeconds(_frameDelays.Average(span => span.TotalSeconds));
        }

        // ReSharper disable once PossibleMultipleEnumeration
        foreach (var frame in processedFrames) 
            frame.Dispose();
        image.Dispose();

        return true;
    }

    private static void ProcessGifFrames(ImageFrameCollection<Rgba32> frames,
                                         ImageFrameCollection<Rgba32> processedFrames, int currentIndex,
                                         GifDisposalMethod disposal)
    {
        void CombineFrames(ImageFrame<Rgba32> sourceFrame, ImageFrame<Rgba32> targetFrame)
        {
            sourceFrame.ProcessPixelRows(targetFrame, (source, target) =>
            {
                for (var row = 0; row < source.Height || row < target.Height; row++)
                {
                    var srcRow = source.GetRowSpan(row);
                    var targetRow = target.GetRowSpan(row);

                    for (var x = 0; x < srcRow.Length || x < targetRow.Length; x++)
                    {
                        ref var srcPixel = ref srcRow[x];
                        ref var targetPixel = ref targetRow[x];

                        if (srcPixel.A > 0)
                        {
                            targetPixel = srcPixel;
                        }
                    }
                }
            });
        }

        if (currentIndex + 1 >= frames.Count)
            return;

        var current = frames[currentIndex];
        var currentProcessed = processedFrames[currentIndex];

        var nextSource = frames[currentIndex + 1];
        ImageFrame<Rgba32> newFrame;

        switch (disposal)
        {
            case GifDisposalMethod.Unspecified:
            case GifDisposalMethod.NotDispose:
            {
                newFrame = processedFrames.AddFrame(currentProcessed);
                break;
            }
            case GifDisposalMethod.RestoreToBackground:
            {
                newFrame = processedFrames.AddFrame(currentProcessed);
                // Clear the areas painted by the current frame before combining with the next
                newFrame.ProcessPixelRows(current, (next, mask) =>
                {
                    for (var row = 0; row < next.Height || row < mask.Height; row++)
                    {
                        var nextRow = next.GetRowSpan(row);
                        var maskRow = mask.GetRowSpan(row);

                        for (var x = 0; x < nextRow.Length || x < maskRow.Length; x++)
                        {
                            ref var nextPixel = ref nextRow[x];
                            ref var maskPixel = ref maskRow[x];

                            if (maskPixel.A > 0)
                            {
                                nextPixel = Color.Transparent;
                            }
                        }
                    }
                });

                break;
            }
            case GifDisposalMethod.RestoreToPrevious:
                var previous = frames.AsEnumerable()
                                     .Take(currentIndex)
                                     .Reverse()
                                     .FirstOrDefault(frame =>
                                                         frame.Metadata.GetGifMetadata().DisposalMethod is not
                                                             GifDisposalMethod
                                                                 .RestoreToPrevious);

                //If no image without previous disposal is found restore to transparent frame
                previous ??= new Image<Rgba32>(nextSource.Width, nextSource.Height, Color.Transparent).Frames[0];
                
                newFrame = processedFrames.AddFrame(previous);
                break;
            default:
                throw new NotSupportedException($"{disposal} is not a valid GIF disposal method");
        }

        CombineFrames(nextSource, newFrame);
    }

    private static unsafe RenderTargetBitmap CopyToBitmap(ImageFrame<Rgba32> frame, AlphaFormat alphaType, Vector dpi)
    {
        var pixelSize = new PixelSize(frame.Width, frame.Height);
        using WriteableBitmap bitmap = new(pixelSize, dpi, PixelFormat.Rgba8888, alphaType);
        using var handle = bitmap.Lock();

        var span = new Span<byte>((void*)handle.Address, handle.Size.Height * handle.RowBytes);

        frame.CopyPixelDataTo(span);

        var renderTarget = new RenderTargetBitmap(pixelSize, dpi);

        using var ctx = renderTarget.CreateDrawingContext();
        ctx.DrawImage(bitmap, new(0, 0, pixelSize.Width, pixelSize.Height));

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

        _timer = new(_averageFrameTime, DispatcherPriority.Render, Tick);
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
        lock (_sync)
        {
            if (CurrentTarget is null) return;

            context.DrawImage(CurrentTarget, sourceRect, destRect);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            Stop();

            foreach (var frame in _targetFrames)
                frame.Dispose();
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

    private void ProcessFrameTime()
    {
        if (!IterationCount.IsInfinite && _iterationCount > IterationCount.Value) return;

        if (_timer is null) return;

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
    }
}