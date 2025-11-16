using Acme.Infrastructure.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Acme.Infrastructure.Extensions;

public static class CorsExtensions
{
    public const string DefaultCorsPolicy = "DefaultCors";

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IOptionsMonitor<CorsSettings> corsOptions)
    {
        var corsSettings = corsOptions.CurrentValue;
        
        services.AddCors(opts =>
        {
            opts.AddPolicy(DefaultCorsPolicy, policy =>
            {
                if (corsSettings.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                          .WithHeaders(corsSettings.AllowedHeaders)
                          .WithMethods(corsSettings.AllowedMethods)
                          .WithExposedHeaders(corsSettings.ExposedHeaders)
                          .SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.MaxAgeSeconds));
                    
                    if (corsSettings.AllowCredentials)
                    {
                        policy.AllowCredentials(); // CRITICAL: Required for httpOnly cookies
                    }
                }
                else
                {
                    policy.WithOrigins();
                }
            });
        });
        return services;
    }

    public static IApplicationBuilder UseConfiguredCors(this IApplicationBuilder app) => app.UseCors(DefaultCorsPolicy);
}
