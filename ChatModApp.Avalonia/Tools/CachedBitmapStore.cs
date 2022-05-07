using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Caching.Memory;

namespace ChatModApp.Tools;

public static class CachedBitmapStore
{
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions{});
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

    private static async Task<Bitmap> BitMapFactory(ICacheEntry entry, Uri uri, CancellationToken cancellationToken = default)
    {
        entry.SetSlidingExpiration(TimeSpan.FromHours(1));
        return await Download(uri, cancellationToken);
    }

    private static async Task<Bitmap> Download(Uri uri, CancellationToken cancellationToken = default)
    {
        var arr = await Client.GetByteArrayAsync(uri, cancellationToken);

        var stream = new MemoryStream(arr);

        return new(stream);
    }
}