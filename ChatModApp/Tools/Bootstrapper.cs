using System;
using System.Linq;
using System.Reflection;
using ChatModApp.Services;
using ChatModApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using Splat.Microsoft.Extensions.Logging;
using Splat.NLog;
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
            
            services
                .AddSingleton<MainViewModel>()
                .AddSingleton<AuthenticationViewModel>()
                .AddSingleton<ChatTabViewModel>()
                .AddTransient<ChatViewModel>()
                .AddTransient<ChatTabItemViewModel>()

                .AddSingleton<TwitchChatService>();
        }
    }
}