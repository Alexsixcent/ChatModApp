using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.Storage;
using ChatModApp.Services;
using ChatModApp.Services.ApiClients;
using ChatModApp.Tools.Extensions;
using ChatModApp.ViewModels;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using ReactiveUI;
using Refit;
using Splat;
using Splat.DryIoc;
using LogLevel = NLog.LogLevel;

namespace ChatModApp.Tools;

public static class Bootstrapper
{
    public static void Init()
    {
        var host = Host
                   .CreateDefaultBuilder()
                   .ConfigureLogging(builder =>
                   {
                       builder.ClearProviders();
                       builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);

                       var config = new LoggingConfiguration();

                       var logConsole = new ConsoleTarget
                       {
                           AutoFlush = true,
                           DetectConsoleAvailable = false
                       };

                       var logFile = new FileTarget
                       {
                           FileName = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "globalLogs.log"),
                           KeepFileOpen = true,
                           AutoFlush = true,
                           ConcurrentWrites = false,
                           DeleteOldFileOnStartup = true
                       };

                       config.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);
                       config.AddRule(LogLevel.Trace, LogLevel.Fatal, logFile);

                       builder.AddNLog(config);
                       builder.AddConsole();
                   })
                   .ConfigureServices(services =>
                   {
                       services
                           .AddRefitClient<IBttvApi>()
                           .ConfigureHttpClient(c => c.BaseAddress = new("https://api.betterttv.net/3/cached"));

                       services
                           .AddRefitClient<IFfzApi>()
                           .ConfigureHttpClient(c => c.BaseAddress = new("https://api.frankerfacez.com/v1"));
                   })
                   .UseEnvironment(Environments.Development)
                   .Build();

        ConfigureServices(host.Services);
    }

    private static void ConfigureServices(IServiceProvider services)
    {
        var container = new Container();
        container.UseDryIocDependencyResolver();

        var resolver = Locator.CurrentMutable;

        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();

        //#TODO: Streamline client registration process, RestEase migration ?
        container.RegisterInstance(services.GetRequiredService<IBttvApi>());
        container.RegisterInstance(services.GetRequiredService<IFfzApi>());
        container.RegisterInstance(services.GetRequiredService<ILoggerFactory>());

        var loggerFactoryMethod = typeof(LoggerFactoryExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                                 .First(
                                                                        info => info.Name == nameof(LoggerFactoryExtensions.CreateLogger) &&
                                                                            info.IsGenericMethod);

        container.Register(typeof(ILogger<>), made: Made.Of(req => loggerFactoryMethod.MakeGenericMethod(req.ServiceType.GenericTypeArguments.First())));

        container.RegisterViewsForViewModels(Assembly.GetExecutingAssembly(), "ChatModApp.Views");


        container.Register<MainViewModel>(Reuse.Singleton);
        container.Register<AuthenticationViewModel>(Reuse.Singleton);
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