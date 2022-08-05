using System;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using AvaloniaGif.Decoding;
using ChatModApp.Shared.Tools.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ChatModApp.Tools;

public static class CachedBitmapStore
{
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
    private static readonly HttpClient Client = new();

    public static async Task<IBitmap?> Get(Uri? uri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri?.AbsoluteUri))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(null, "URI cannot be null or empty");
            return null;
        }

        switch (uri.Scheme)
        {
            case "http":
            case "https":
                return await Cache.GetOrCreateAsync(uri, entry => BitMapFactory(entry, uri, cancellationToken));
            case "avares":
                throw new NotImplementedException();
            default:
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                      ?.Log(null, "Cached bitmaps only supports HTTP/HTTPS or avares:// URI schemes");
                return null;
        }
    }

    private static async Task<IBitmap> BitMapFactory(ICacheEntry entry, Uri uri,
                                                     CancellationToken cancellationToken = default)
    {
        entry.SetSlidingExpiration(TimeSpan.FromHours(1))
             .RegisterPostEvictionCallback((key, value, reason, _) =>
             {
                 if (reason is not (EvictionReason.Capacity or EvictionReason.Expired) ||
                     value is not IDisposable d) return;
                 
                 d.Dispose();
                 Log.Debug("Bitmap expired, Reason: {Reason}, Uri: {BitMapUri}", reason, key);
             });
        return await Download(uri, cancellationToken);
    }

    private static async Task<IBitmap> Download(Uri uri, CancellationToken cancellationToken = default)
    {
        var arr = await Observable.FromAsync(() => Client.GetByteArrayAsync(uri, cancellationToken))
                                  .RetryWithBackoffStrategy<byte[], HttpRequestException>(5, TimeSpan.FromSeconds(2));

        var stream = new MemoryStream(arr);

        IBitmap bitmap;
        try
        {
            bitmap = new ImageGifBitmap(stream);
        }
        catch (InvalidGifStreamException _)
        {
            Log.Verbose("Image {Uri} is not a GIF switching to still image bitmap", uri);
            stream.Position = 0; //Reset stream to prevent exception
            bitmap = new Bitmap(stream);
        }

        return bitmap;
    }
}