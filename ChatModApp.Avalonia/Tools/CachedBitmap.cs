using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ChatModApp.Tools;

public static class CachedBitmapStore
{
    private static readonly ConcurrentDictionary<Uri, Task<Bitmap>> CachedBitmap = new();
    private static readonly HttpClient Client = new();

    public static async Task<IImage> Get(Uri uri, CancellationToken cancellationToken = default)
    {
        if (CachedBitmap.TryGetValue(uri, out var cached))
        {
            return await cached;
        }

        var task = Download(uri, cancellationToken);

        CachedBitmap[uri] = task;

        return await task;
    }

    private static async Task<Bitmap> Download(Uri uri, CancellationToken cancellationToken)
    {
        var arr = await Client.GetByteArrayAsync(uri, cancellationToken).ConfigureAwait(false);

        var stream = new MemoryStream(arr);
        
        return new(stream);
    }
}