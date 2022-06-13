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

namespace ChatModApp.Tools;

public class ImageGifBitmap : IBitmap
{
    public Size Size { get; }
    public IRef<IBitmapImpl> PlatformImpl { get; }

    public PixelSize PixelSize { get; }
    public Vector Dpi { get; }
    
    private readonly GifInstance _instance;
    private readonly RenderTargetBitmap _bitmap;
    private readonly Stopwatch _stopwatch;

    public ImageGifBitmap(Stream stream)
    {
        _instance = new(stream);
        _instance.IterationCount = IterationCount.Infinite; //TODO: Find a way to pass that down from control
        _bitmap = new(_instance.GifPixelSize, new(96, 96));
        
        _stopwatch = new();
        _stopwatch.Reset();

        Size = _bitmap.Size;
        PlatformImpl = _bitmap.PlatformImpl;
        PixelSize = _bitmap.PixelSize;
        Dpi = _bitmap.Dpi;
    }

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect,
                     BitmapInterpolationMode bitmapInterpolationMode)
    {
        if (_instance.CurrentCts?.IsCancellationRequested ?? true)
            return;

        if (!_stopwatch.IsRunning) _stopwatch.Start();

        var currentFrame = _instance.ProcessFrameTime(_stopwatch.Elapsed);
        if (currentFrame is { } source)
        {
            using var ctx = _bitmap.CreateDrawingContext(null);
            var ts = new Rect(source.Size);
            ctx.DrawBitmap(source.PlatformImpl, 1, ts, ts);
        }

        context.DrawImage(_bitmap, sourceRect, destRect, bitmapInterpolationMode);
    }

    public void Dispose()
    {
        _instance.Dispose();
        _bitmap.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Save(string fileName) => _bitmap.Save(fileName);

    public void Save(Stream stream) => _bitmap.Save(stream);
}