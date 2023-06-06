using System;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaGif;
using ChatModApp.Shared.Tools.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ChatModApp.Tools;

public static class CachedBitmapStore
{
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
    private static readonly HttpClient Client = new();

    public static async Task<IImage?> Get(Uri? uri, Uri? baseUri = null, CancellationToken cancellationToken = default)
    {
        if (uri is null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(null, "URI cannot be null or empty");
            return null;
        }

        switch (uri.Scheme)
        {
            case "http":
            case "https":
                if (!string.IsNullOrWhiteSpace(uri.AbsoluteUri))
                    return await Cache.GetOrCreateAsync(uri, entry => BitMapFactory(entry, uri, cancellationToken));

                Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                      ?.Log(null, "Https AbsoluteUri cannot be null or empty");
                return null;

            case "avares":
                try
                {
                    if (AssetLoader.Exists(uri, baseUri))
                        return new Bitmap(AssetLoader.Open(uri, baseUri));
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                          ?.Log(null, "Error while loading bitmap from asset loader: {Exception}", ex);
                }

                return null;
            default:
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                      ?.Log(null, "Cached bitmaps only supports HTTP/HTTPS or avares:// URI schemes");
                return null;
        }
    }

    private static async Task<IImage> BitMapFactory(ICacheEntry entry, Uri uri,
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

    private static async Task<IImage> Download(Uri uri, CancellationToken cancellationToken = default)
    {
        var arr = await Observable.FromAsync(() => Client.GetByteArrayAsync(uri, cancellationToken))
                                  .RetryWithBackoffStrategy<byte[], HttpRequestException>(5, TimeSpan.FromSeconds(2));

        var stream = new MemoryStream(arr);
        var bitmap = new AnimatedBitmap();

        if (bitmap.Load(stream, cancellationToken)) return bitmap;
        
        bitmap.Dispose();
        stream.Position = 0;
        return new Bitmap(stream);
    }
}