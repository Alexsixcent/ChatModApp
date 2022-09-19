using System.Diagnostics;
using System.Reactive.Linq;
using ChatModApp.AuthCallback;
using ChatModApp.Shared.Tools.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ChatModApp.Shared.Services;

public sealed class BlazorHostingService : IDisposable, IAsyncDisposable
{
    public string? CurrentHostingUrl { get; private set; }
    public IObservable<AuthenticatedEventArgs> AuthFromBlazor { get; private set; } = null!;


    private const string SolutionName = "ChatModApp", BlazorAppName = "ChatModApp.AuthCallback";

    private static string[] HostingUrls => new[]
    {
        "https://localhost:5244",
        "https://localhost:5000",
        "https://localhost:5001",
        "https://localhost:7167"
    };


    private readonly ILogger<BlazorHostingService> _logger;
    private readonly WebApplicationOptions _webOptions;
    private WebApplication? _webApplication;

    public BlazorHostingService(ILogger<BlazorHostingService> logger)
    {
        _logger = logger;
        _webOptions = CreateOptions();
    }

    private static WebApplicationOptions CreateOptions(string solutionName = SolutionName, string blazorAppName = BlazorAppName)
    {
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

        return new()
        {
            ApplicationName = blazorAppName,
            WebRootPath = webRoot,
            ContentRootPath = contentRoot
        };
    }

    private WebApplication BuildApplication(string hostingUrl)
    {
        var builder = StartupHelpers.CreateBuilder(_webOptions);
        builder.Host.UseSerilog();
        
        var webApplication = StartupHelpers.CreateApplication(builder);
        webApplication.Urls.Add(hostingUrl);
        
        return webApplication;
    }

    internal async Task StartBlazor()
    {
        bool retryWithUrl;
        var currentUrlIndex = 0;
        do
        {
            _webApplication = BuildApplication(HostingUrls[currentUrlIndex]);
            try
            {
                await _webApplication.StartAsync();
                retryWithUrl = false;
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Couldn't start Blazor server at {Url}, retrying...", HostingUrls[currentUrlIndex]);
                retryWithUrl = true;
                currentUrlIndex++;
                if (currentUrlIndex >= HostingUrls.Length)
                {
                    _logger.LogCritical("No available remaining URL for hosting the Blazor authentication were found");
                    throw;
                }

                await _webApplication.DisposeAsync();
                _logger.LogInformation("Restarting Blazor server at {Url}", HostingUrls[currentUrlIndex]);
            }
        } while (retryWithUrl);

        var authManager = _webApplication.Services.GetService<AuthTriggeredService>();

        Debug.Assert(authManager != null, nameof(authManager) + " != null");

        AuthFromBlazor = Observable.FromEventPattern<AuthenticatedEventArgs>(h => authManager.Authenticated += h,
                                                                             h => authManager.Authenticated -= h)
                                   .ObserveOnThreadPool()
                                   .Select(pattern => pattern.EventArgs);
        CurrentHostingUrl = HostingUrls[currentUrlIndex];
    }


    public void Dispose()
    {
        ((IDisposable?)_webApplication)?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await (_webApplication?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}