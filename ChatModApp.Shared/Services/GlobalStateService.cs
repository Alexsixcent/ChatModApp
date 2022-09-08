using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using ChatModApp.AuthCallback;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace ChatModApp.Shared.Services;

public class GlobalStateService : IDisposable
{
    public ImmutableHashSet<string> TLDs { get; private set; }

    private WebApplication? _webApplication;

    public async Task Initialize()
    {
        TLDs = ImmutableHashSet.CreateRange(await GetTLDs());

        await StartBlazor();
    }

    private async Task StartBlazor()
    {
        const string solutionName = "ChatModApp", blazorAppName = "ChatModApp.AuthCallback";

        //From https://github.com/dotnet/aspnetcore/issues/35648#issuecomment-904728134
        var curr = Directory.GetCurrentDirectory();
        var isContentRootPresent = Directory.EnumerateDirectories(curr, "wwwroot").Any();
        var pIndex = curr.LastIndexOf(solutionName + Path.DirectorySeparatorChar, StringComparison.Ordinal);

        string contentRoot;
        if (isContentRootPresent)
        {
            contentRoot = Path.Combine(curr, "wwwroot");
        }
        else if (pIndex < 0)
        {
            contentRoot = curr;
        }
        else
        {
            var solDir = curr[..(pIndex + solutionName.Length)];
            contentRoot = Path.Combine(solDir, blazorAppName);
        }

        var options = new WebApplicationOptions
        {
            ApplicationName = blazorAppName,
            ContentRootPath = contentRoot
        };

        var builder = StartupHelpers.CreateBuilder(options);
        builder.Host.UseSerilog();
        _webApplication = StartupHelpers.CreateApplication(builder);

        await _webApplication.StartAsync();
    }

    private static async Task<IEnumerable<string>> GetTLDs()
    {
        var mapping = new IdnMapping();
        using var client = new WebClient();

        var tldText = await client.DownloadStringTaskAsync("https://data.iana.org/TLD/tlds-alpha-by-domain.txt");

        return tldText
            .Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(s => mapping.GetUnicode(s.ToLowerInvariant()));
    }

    public void Dispose()
    {
        ((IDisposable?) _webApplication)?.Dispose();
    }
}