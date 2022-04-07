using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ChatModApp.Services;

public class GlobalStateService
{
    public ImmutableHashSet<string> TLDs { get; private set; } 

    public async Task Initialize()
    {
        TLDs = ImmutableHashSet.CreateRange(await GetTLDs());
    }

    private static async Task<IEnumerable<string>> GetTLDs()
    {
        var mapping = new IdnMapping();
        using var client = new WebClient();

        var tldText = await client.DownloadStringTaskAsync("https://data.iana.org/TLD/tlds-alpha-by-domain.txt");

        return tldText
               .Split('\n', StringSplitOptions.RemoveEmptyEntries)
               .Skip(1)
               .Select(s => mapping.GetUnicode(s.ToLowerInvariant()));
    }
}