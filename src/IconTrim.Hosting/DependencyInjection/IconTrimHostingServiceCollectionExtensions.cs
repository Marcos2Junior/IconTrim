using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace IconTrim.Hosting.DependencyInjection;

/// <summary>Enables automatic execution during Generic Host startup.</summary>
public static class IconTrimHostingServiceCollectionExtensions
{
    /// <summary>Registers one hosted service that runs IconTrim once while the host starts.</summary>
    /// <param name="services">The consumer's service collection.</param>
    /// <returns>The same service collection for further registrations.</returns>
    /// <remarks>The host awaits generation in <c>StartAsync</c>; errors propagate and prevent normal startup. Registration is idempotent and performs no generation. The service does not run a loop or continue in the background. The consumer decides in which environments to register it.</remarks>
    public static IServiceCollection AddIconTrimOnStartup(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, IconTrimHostedService>());
        return services;
    }
}
