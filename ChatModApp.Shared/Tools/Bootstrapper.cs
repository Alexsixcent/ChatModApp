using System.Reactive;
using System.Reflection;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Services.ApiClients;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Shared.ViewModels;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReactiveUI;
using Refit;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Splat;
using Splat.DryIoc;
using Splat.Serilog;

namespace ChatModApp.Shared.Tools;

public static class Bootstrapper
{
    public static void Init(string logFolder, IContainer? container = null)
    {
        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.Debug()
                     .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                     .MinimumLevel.Override("TwitchLib", LogEventLevel.Debug)
                     .Enrich.FromLogContext()
                     .WriteTo.Console()
                     .WriteTo.Debug()
                     .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logFolder, "globalLogs.log"))
                     .CreateLogger();
        
        var host = Host
                   .CreateDefaultBuilder()
                   .ConfigureServices(services =>
                   {
                       services
                           .AddRefitClient<IBttvApi>()
                           .ConfigureHttpClient(c => c.BaseAddress = new("https://api.betterttv.net/3/cached"));

                       services
                           .AddRefitClient<IFfzApi>()
                           .ConfigureHttpClient(c => c.BaseAddress = new("https://api.frankerfacez.com/v1"));
                   })
                   .UseSerilog()
                   .UseEnvironment(Environments.Development)
                   .Build();

        ConfigureServices(host.Services, container ?? new Container());
        RxApp.DefaultExceptionHandler =
            Observer.Create<Exception>(ex => Log.Fatal(ex, "Unhandled exception occurred in observable"));
    }

    private static void ConfigureServices(IServiceProvider services, IContainer container)
    {
        container.UseDryIocDependencyResolver();

        var resolver = Locator.CurrentMutable;
        
        resolver.UseSerilogFullLogger();
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();
        resolver.RegisterConstant(new JsonSerializerSettings{TypeNameHandling = TypeNameHandling.Auto});

        //#TODO: Streamline client registration process, RestEase migration ?
        container.RegisterInstance(services.GetRequiredService<IBttvApi>());
        container.RegisterInstance(services.GetRequiredService<IFfzApi>());
        container.RegisterInstance(services.GetRequiredService<ILoggerFactory>());

        var loggerFactoryMethod = typeof(LoggerFactoryExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                                 .First(
                                                                        info => info.Name == nameof(LoggerFactoryExtensions.CreateLogger) &&
                                                                            info.IsGenericMethod);

        container.Register(typeof(ILogger<>), made: Made.Of(req => loggerFactoryMethod.MakeGenericMethod(req.ServiceType.GenericTypeArguments.First())));

        container.RegisterViewsForViewModels(Assembly.GetEntryAssembly() ?? throw new PlatformNotSupportedException("Couldn't find entry application assembly where views are defined"), "ChatModApp.Views");


        container.Register<AuthenticationViewModel>(Reuse.Singleton);
        container.Register<MainViewModel>(Reuse.Singleton);
        container.Register<ChatTabViewModel>(Reuse.Singleton);

        container.Register<GlobalStateService>(Reuse.Singleton);
        container.Register<AuthenticationService>(Reuse.Singleton);
        container.Register<TwitchApiService>(Reuse.Singleton);
        container.Register<TwitchChatService>(Reuse.Singleton);
        container.Register<EmotesService>(Reuse.Singleton);
        container.Register<MessageProcessingService>(Reuse.Singleton);
        container.Register<ChatTabService>(Reuse.Singleton);

        container.Register<ChatViewModel>(setup: Setup.With(allowDisposableTransient: true));
        container.Register<ChatTabItemViewModel>(setup: Setup.With(allowDisposableTransient: true));
        container.Register<ChatTabPromptViewModel>(setup: Setup.With(allowDisposableTransient: true));
    }
}