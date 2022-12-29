using System.Collections.Immutable;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ChatModApp.Shared.Tools.Extensions;

namespace ChatModApp.Shared.Services;

public sealed class GlobalStateService
{
    public ImmutableHashSet<string> TLDs { get; private set; }


    private readonly HttpClient _client;
    private readonly BlazorHostingService _blazorService;
    private readonly EmotesService _emotesService;


    public GlobalStateService(BlazorHostingService blazorService, EmotesService emotesService, HttpClient client)
    {
        _blazorService = blazorService;
        _emotesService = emotesService;
        _client = client;
        TLDs = ImmutableHashSet<string>.Empty;
    }

    public async Task Initialize()
    {
        var tldObv = GetTLDs().ToArray().ToTask();

        _emotesService.Initialize();

        var tlds = await tldObv;
        
        TLDs = ImmutableHashSet.CreateRange(tlds);
        
        if (!BlazorHostingService.IsBlazorAuthDisabled)
            await _blazorService.StartBlazor();
    }

    private IObservable<string> GetTLDs()
    {
        var mapping = new IdnMapping();

        return Observable
               .FromAsync(token => _client.GetStreamAsync("https://data.iana.org/TLD/tlds-alpha-by-domain.txt",
                                                          token))
               .ReadLinesToEnd()
               .Skip(1)
               .WhereNotNullOrWhiteSpace()
               .Select(s => mapping.GetUnicode(s.ToLowerInvariant()));
    }
}