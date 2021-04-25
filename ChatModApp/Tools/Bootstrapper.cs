using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ChatModApp.Services;
using ChatModApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;
using ReactiveUI;
using Refit;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using Splat.Microsoft.Extensions.Logging;
using Splat.NLog;
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
                .ConfigureLogging(loggingBuilder =>
                {
                    //remove loggers incompatible with UWP
                    {
                        var eventLoggers = loggingBuilder.Services
                            .Where(l => l.ImplementationType == typeof(EventLogLoggerProvider))
                            .ToList();

                        foreach (var el in eventLoggers)
                            loggingBuilder.Services.Remove(el);
                    }

                    InitLogger();
                    loggingBuilder.AddSplat();
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


        private static void InitLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logConsole = new NLog.Targets.ConsoleTarget();

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);

            NLog.LogManager.Configuration = config;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var resolver = Locator.CurrentMutable;
            resolver.InitializeSplat();
            resolver.InitializeReactiveUI();
            resolver.UseNLogWithWrappingFullLogger();

            resolver.RegisterViewsForViewModels(Assembly.GetExecutingAssembly(), "ChatModApp.Views");

            services.AddRefitClient<IAuthApi>()
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