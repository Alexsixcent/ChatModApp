using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tools.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddHostedSingletonService<THostedService>(this IServiceCollection services)
        where THostedService : class, IHostedService
    {
        return services
               .AddSingleton<THostedService>()
               .AddHostedService(provider => provider.GetRequiredService<THostedService>());
    }
}