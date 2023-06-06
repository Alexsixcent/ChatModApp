#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace ChatModApp.Tools;

public sealed class AnimatedBitmap : IImage, IDisposable
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


    public AnimatedBitmap()
    {
        _sync = new();
        _watch = new();
        _targetFrames = Array.Empty<RenderTargetBitmap>();
        _frameDelays = new();
        _totalTime = TimeSpan.Zero;
        _currentFrameIndex = -1;
    }


    public bool Load(Stream stream, CancellationToken token = default)
    {
        stream.Position = 0;

        if (token.IsCancellationRequested) return false;

        using var codec = SKCodec.Create(stream, out var result);
        if (result is not SKCodecResult.Success)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Platform)
                  ?.Log(this, "Skia decoder unable to decode stream, error = {Result}", result);
            return false;
        }

        if (token.IsCancellationRequested) return false;

        var frameCount = codec.FrameCount;

        _targetFrames = new RenderTargetBitmap[frameCount];

        var pixelSize = new PixelSize(codec.Info.Width, codec.Info.Height);
        var totalDuration = TimeSpan.Zero;
        var durations = new TimeSpan[frameCount];
        var accumulatedDurations = new TimeSpan[frameCount];
        var dpi = new Vector(96, 96);

        using var writeable = new WriteableBitmap(pixelSize, dpi, null, codec.Info.AlphaType.ToAlphaFormat());
        using var handle = writeable.Lock();
        var imageInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height,
                                        writeable.Format?.ToSkColorType() ?? SKColorType.Bgra8888,
                                        codec.Info.AlphaType, codec.Info.ColorSpace);

        if (frameCount == 0)
        {
            var res = codec.GetPixels(imageInfo, handle.Address);
            if (res is not SKCodecResult.Success)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Platform)
                      ?.Log(this, "Unable to copy decoded first frame to target bitmap, error = {Result}", result);
                return false;
            }

            _targetFrames = new[] { new RenderTargetBitmap(pixelSize) };
            using var ctx = _targetFrames[0].CreateDrawingContext();

            ctx.DrawImage(writeable, new(0, 0, codec.Info.Width, codec.Info.Height));
        }

        for (var i = 0; i < frameCount; i++)
        {
            if (token.IsCancellationRequested)
                return false;

            var frameInfo = codec.FrameInfo[i];
            durations[i] = TimeSpan.FromSeconds(Math.Max(frameInfo.Duration / 1000.0, MaxFrameTime));

            var codecOptions = new SKCodecOptions(i);

            var res = codec.GetPixels(imageInfo, handle.Address, codecOptions);
            if (res is not SKCodecResult.Success)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Platform)
                      ?.Log(this, "Unable to copy decoded frame to target bitmap, error = {Result}", result);
                return false;
            }

            _targetFrames[i] = new(pixelSize);
            using var ctx = _targetFrames[i].CreateDrawingContext();

            ctx.DrawImage(writeable, new(0, 0, codec.Info.Width, codec.Info.Height));
        }

        totalDuration = durations.Aggregate(totalDuration, (current, t) => current + t);

        for (var i = 0; i < durations.Length; i++)
        {
            accumulatedDurations[i] = durations[i] + (i == 0 ? TimeSpan.Zero : accumulatedDurations[i - 1]);
        }

        PixelSize = pixelSize;
        Dpi = dpi;
        _frameDelays = durations.ToList();
        _totalTime = totalDuration;

        if (_frameDelays.Count > 0)
        {
            _averageFrameTime = TimeSpan.FromSeconds(_frameDelays.Average(span => span.TotalSeconds));
        }

        return true;
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