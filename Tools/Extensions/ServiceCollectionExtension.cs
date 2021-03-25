using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tools.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddHostedSingletonService<THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            return services
                .AddSingleton<THostedService>()
                .AddHostedService(provider => provider.GetRequiredService<THostedService>());
        }

        public static IServiceCollection AddService<TService>(this IServiceCollection services)
            where TService : class, IService
        {
            return services
                    .AddSingleton<TService>()
                    .AddSingleton<IService, TService>(provider => provider.GetRequiredService<TService>());
        }
    }
}