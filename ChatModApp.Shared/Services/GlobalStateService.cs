using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reactive.Linq;
using ChatModApp.AuthCallback;
using ChatModApp.Shared.Tools.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ChatModApp.Shared.Services;

public sealed class GlobalStateService : IDisposable
{
    public ImmutableHashSet<string> TLDs { get; private set; }

    public IObservable<AuthenticatedEventArgs> AuthFromBlazor { get; private set; }

    
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
        var isWebRootPresent = Directory.EnumerateDirectories(curr, "wwwroot").Any();
        var pIndex = curr.LastIndexOf(solutionName + Path.DirectorySeparatorChar, StringComparison.Ordinal);

        string webRoot;
        string contentRoot;
        if (isWebRootPresent)
        {
            webRoot = Path.Combine(curr, "wwwroot");
            contentRoot = Directory.EnumerateDirectories(webRoot, "_content")
                                   .SingleOrDefault() ?? curr;
        }
        else if (pIndex < 0)
        {
            //Falls back to current directory
            webRoot = curr;
            contentRoot = curr;
        }
        else
        {
            //Sets content root to project directory
            var solDir = curr[..(pIndex + solutionName.Length)];
            contentRoot = Path.Combine(solDir, blazorAppName);
            webRoot = Path.Combine(contentRoot, "wwwroot");
        }

        var options = new WebApplicationOptions
        {
            ApplicationName = blazorAppName,
            WebRootPath = webRoot,
            ContentRootPath = contentRoot
        };

        var builder = StartupHelpers.CreateBuilder(options);
        builder.WebHost.UseUrls("https://localhost:5000",
                                "https://localhost:5001",
                                "https://localhost:5244",
                                "https://localhost:7167");
        builder.Host.UseSerilog();
        _webApplication = StartupHelpers.CreateApplication(builder);

        await _webApplication.StartAsync();

        var authManager = _webApplication.Services.GetService<AuthTriggeredService>();

        Debug.Assert(authManager != null, nameof(authManager) + " != null");

        AuthFromBlazor = Observable.FromEventPattern<AuthenticatedEventArgs>(h => authManager.Authenticated += h,
                                                            h => authManager.Authenticated -= h)
                  .ObserveOnThreadPool()
                  .Select(pattern => pattern.EventArgs);
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