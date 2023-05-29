using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Services.ApiClients;
using ChatModApp.Shared.Services.EmoteProviders;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Shared.ViewModels;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using Refit;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Splat;
using Splat.DryIoc;
using Splat.Serilog;

namespace ChatModApp.Shared.Tools;

public class Bootstrapper : IDisposable
{
    private readonly string _appName;
    private readonly string _logFolder;
    private IHostBuilder _hostBuilder;
    private IHost? _activeHost;

    public Bootstrapper(string applicationName, IHostBuilder? builder = null)
    {
        _appName = applicationName;
        _hostBuilder = builder ?? Host.CreateDefaultBuilder();
        _logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), applicationName);
    }

    public Bootstrapper Configure(Action<IContainer>? additionalReg = null)
    {
        Log.Logger = CreateLoggerConfig(_logFolder).CreateLogger();
        
        SetupSuspensionHost();

        _hostBuilder = _hostBuilder
            .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container()))
            .ConfigureContainer<IContainer>(container =>
            {
                additionalReg?.Invoke(container);
                ConfigureServices(container);
            })
            .ConfigureServices(ConfigureHostServices)
            .UseSerilog()
            .UseEnvironment(Environments.Development);
        
        RxApp.DefaultExceptionHandler =
            Observer.Create<Exception>(ex => Log.Fatal(ex, "Unhandled exception occurred in observable"));

        return this;
    }

    public async Task Start(string[] args, CancellationToken token = default)
    {
        _activeHost = await _hostBuilder.ConfigureHostConfiguration(builder => builder.AddCommandLine(args))
            .StartAsync(token);
    }

    public async Task Stop(CancellationToken token = default)
    {
        if (_activeHost is null) return;
        await _activeHost.StopAsync(token);

        if (_activeHost is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
        else
            _activeHost.Dispose();
    }

    public void Dispose()
    {
        _activeHost?.Dispose();
    }

    private static void SetupSuspensionHost()
    {
        RxApp.SuspensionHost.CreateNewAppState = () => new AppState();
        RxApp.SuspensionHost.SetupDefaultSuspendResume(new AkavacheSuspensionDriver<AppState>());
        RxApp.SuspensionHost.WhenAnyValue(host => host.AppState)
            .WhereNotNull()
            .Cast<AppState>()
            .Subscribe(state => Locator.CurrentMutable.RegisterConstant(state));
    }

    private static void ConfigureHostServices(IServiceCollection services)
    {
        services.AddRefitClient<IBttvApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new("https://api.betterttv.net/3/cached"));

        services.AddRefitClient<IFfzApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new("https://api.frankerfacez.com/v1"));
    }

    private static void ConfigureServices(IContainer container)
    {
        container.UseDryIocDependencyResolver();

        var resolver = Locator.CurrentMutable;

        resolver.UseSerilogFullLogger();
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();
        
        container.RegisterViewsForViewModels(Assembly.GetEntryAssembly() ?? throw new PlatformNotSupportedException("Couldn't find entry application assembly where views are defined"),
                                             "ChatModApp.Views");

        //Scans assembly for all types implementing IEmoteProvider and register them as singletons
        container.RegisterMany(new []{typeof(IEmoteProvider).Assembly}, serviceTypeCondition:type => type == typeof(IEmoteProvider), Reuse.Singleton);

        container.Register<AuthenticationViewModel>(Reuse.Singleton);
        container.Register<MainViewModel>(Reuse.Singleton);
        container.Register<ChatTabViewModel>(Reuse.Singleton);

        container.Register<GlobalStateService>(Reuse.Singleton);
        container.Register<AuthenticationService>(Reuse.Singleton);
        container.Register<BlazorHostingService>(Reuse.Singleton);
        container.Register<TwitchApiService>(Reuse.Singleton);
        container.Register<TwitchChatService>(Reuse.Singleton);
        container.Register<EmotesService>(Reuse.Singleton);
        container.Register<MessageProcessingService>(Reuse.Singleton);
        container.Register<ChatTabService>(Reuse.Singleton);
        container.Register<EmotePickerViewModel>(Reuse.Singleton);

        container.Register<ChatViewModel>();
        container.Register<ChatTabItemViewModel>(setup: Setup.With(allowDisposableTransient: true));
        container.Register<ChatTabPromptViewModel>(setup: Setup.With(allowDisposableTransient: true));
        container.Register<UserListViewModel>(setup: Setup.With(allowDisposableTransient: true));
    }

    private static LoggerConfiguration CreateLoggerConfig(string folder, LoggerConfiguration? config = null) 
        => (config ?? new LoggerConfiguration())
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("TwitchLib", LogEventLevel.Debug)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Debug()
        .WriteTo.File(new CompactJsonFormatter(), Path.Combine(folder, "globalLogs.log"));
}