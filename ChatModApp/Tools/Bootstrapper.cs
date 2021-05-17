using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;
using ChatModApp.Services;
using ChatModApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using ReactiveUI;
using Refit;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using Tools;
using Tools.Extensions;
using LogLevel = NLog.LogLevel;
using Registrations = Akavache.Registrations;

namespace ChatModApp.Tools
{
    public static class Bootstrapper
    {
        public static IServiceProvider Container { get; private set; }

        public static void Init()
        {
            Registrations.Start("ChatModApp");

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

                    var logFile = new FileTarget{
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
                    services.UseMicrosoftDependencyResolver();

                    ConfigureServices(services);
                })
                .UseEnvironment(Environments.Development)
                .Build();

            Container = host.Services;
            Container.UseMicrosoftDependencyResolver();
        }

        public static Task InitServices()
        {
            var tasks = Container.GetServices<IService>()
                .Select(s => s.Initialize());

            return Task.WhenAll(tasks);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var resolver = Locator.CurrentMutable;
            resolver.InitializeSplat();
            resolver.InitializeReactiveUI();

            resolver.RegisterViewsForViewModels(Assembly.GetExecutingAssembly(), "ChatModApp.Views");

            services
                .AddRefitClient<IAuthApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://id.twitch.tv"));

            services
                .AddSingleton<MainViewModel>()
                .AddSingleton<AuthenticationViewModel>()
                .AddSingleton<ChatTabViewModel>()
                .AddTransient<ChatViewModel>()
                .AddTransient<ChatTabItemViewModel>()
                .AddTransient<ChatTabPromptViewModel>()

                .AddService<AuthenticationService>()
                .AddService<TwitchApiService>()
                .AddService<TwitchChatService>()
                .AddService<EmotesService>();
        }
    }
}