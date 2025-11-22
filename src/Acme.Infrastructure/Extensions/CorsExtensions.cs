namespace Acme.Infrastructure.Extensions;

using Acme.Infrastructure.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                // Check if we have a wildcard origin
                bool allowAnyOrigin = corsSettings.AllowedOrigins.Contains("*");

                if (allowAnyOrigin)
                {
                    policy.AllowAnyOrigin()
                          .WithHeaders(corsSettings.AllowedHeaders)
                          .WithMethods(corsSettings.AllowedMethods)
                          .WithExposedHeaders(corsSettings.ExposedHeaders)
                          .SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.MaxAgeSeconds));
                    
                    // CRITICAL: Cannot use AllowCredentials() with AllowAnyOrigin()
                }
                else if (corsSettings.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                          .WithHeaders(corsSettings.AllowedHeaders)
                          .WithMethods(corsSettings.AllowedMethods)
                          .WithExposedHeaders(corsSettings.ExposedHeaders)
                          .SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.MaxAgeSeconds));
                    
                    if (corsSettings.AllowCredentials)
                    {
                        policy.AllowCredentials(); // Safe to use with specific origins
                    }
                }
                else
                {
                    // Fallback: deny all if no origins configured (safe default)
                    policy.WithOrigins(); 
                }
            });
        });
        return services;
    }

    public static IApplicationBuilder UseConfiguredCors(this IApplicationBuilder app) => app.UseCors(DefaultCorsPolicy);
}
