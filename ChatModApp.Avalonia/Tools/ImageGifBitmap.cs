using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaGif;

namespace ChatModApp.Tools;

public sealed class ImageGifBitmap : RenderTargetBitmap
{
    private readonly GifInstance _instance;
    private readonly Stopwatch _stopwatch;

    public ImageGifBitmap(Stream stream) : this(new GifInstance(stream))
    { }
    
    private ImageGifBitmap(GifInstance instance) : base(instance.GifPixelSize, new(96, 96))
    {
        _instance = instance;
        _instance.IterationCount = IterationCount.Infinite; //TODO: Find a way to pass that down from control

        _stopwatch = new();
        _stopwatch.Reset();
    }

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        if (_instance.CurrentCts?.IsCancellationRequested ?? true)
            return;

        if (!_stopwatch.IsRunning) _stopwatch.Start();

        var currentFrame = _instance.ProcessFrameTime(_stopwatch.Elapsed);
        if (currentFrame is null) return;
        
        var ts = new Rect(currentFrame.Size);
        context.DrawImage(currentFrame, ts, ts);
    }

    public override void Dispose()
    {
        _instance.Dispose();
        base.Dispose();
    }
}

