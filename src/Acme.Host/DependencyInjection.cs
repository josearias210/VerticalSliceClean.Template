using Acme.Host.Extensions;

namespace Acme.Host;

public static class DependencyInjection
{
    /// <summary>
    /// Configures host-level services (ASP.NET Core specific features).
    /// </summary>
    public static IServiceCollection AddHost(this IServiceCollection services, IConfiguration configuration)
    {
        // Rate Limiting (moved to extension for better organization)
        services.AddConfiguredRateLimiting();
        
        return services;
    }
}
