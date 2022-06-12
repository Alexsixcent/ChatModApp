using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;
using AvaloniaGif;
using AvaloniaGif.Decoding;

namespace ChatModApp.Tools;

public class ImageGifBitmap : IBitmap
{
    public Size Size { get; }
    public IRef<IBitmapImpl> PlatformImpl { get; }

    public PixelSize PixelSize { get; }
    public Vector Dpi { get; }
    
    private readonly GifInstance? _instance;
    private readonly RenderTargetBitmap? _gifBitmap;
    private readonly IBitmap? _still;
    private readonly Stopwatch _stopwatch;

    public ImageGifBitmap(Stream stream)
    {
        _stopwatch = new();
        _stopwatch.Reset();

        try
        {
            _instance = new(stream);
            _instance.IterationCount = IterationCount.Infinite; //Find a way to pass that down from control
            _gifBitmap = new(_instance.GifPixelSize, new(96, 96));

            Size = _gifBitmap.Size;
            PlatformImpl = _gifBitmap.PlatformImpl;
            PixelSize = _gifBitmap.PixelSize;
            Dpi = _gifBitmap.Dpi;
        }
        catch (InvalidGifStreamException e)
        {
            _instance?.Dispose();
            _instance = null;

            stream.Position = 0;
            _still = new Bitmap(stream);
            
            Size = _still.Size;
            PlatformImpl = _still.PlatformImpl;
            PixelSize = _still.PixelSize;
            Dpi = _still.Dpi;
        }
    }

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect,
                     BitmapInterpolationMode bitmapInterpolationMode)
    {
        if (_instance is null || (_instance.CurrentCts?.IsCancellationRequested ?? true))
        {
            _still?.Draw(context, sourceRect, destRect, bitmapInterpolationMode);
            return;
        }

        if (!_stopwatch.IsRunning) _stopwatch.Start();

        var currentFrame = _instance.ProcessFrameTime(_stopwatch.Elapsed);
        if (currentFrame is { } source && _gifBitmap is { })
        {
            using var ctx = _gifBitmap.CreateDrawingContext(null);
            var ts = new Rect(source.Size);
            ctx.DrawBitmap(source.PlatformImpl, 1, ts, ts);
        }

        if (_gifBitmap is not null)
        {
            context.DrawImage(_gifBitmap, sourceRect, destRect, bitmapInterpolationMode);
        }
    }

    public void Dispose()
    {
        _instance?.Dispose();
        _gifBitmap?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Save(string fileName)
    {
        _gifBitmap?.Save(fileName);
        _still?.Save(fileName);
    }

    public void Save(Stream stream)
    {
        _gifBitmap?.Save(stream);
        _still?.Save(stream);
    }
}