using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Infrastructure.Extensions;

public static class HttpLoggingExtensions
{
    public static IServiceCollection AddSelectiveHttpLogging(this IServiceCollection services)
    {
        services.AddHttpLogging(options =>
        {
            options.LoggingFields =
                HttpLoggingFields.RequestPath |
                HttpLoggingFields.RequestMethod |
                HttpLoggingFields.RequestScheme |
                HttpLoggingFields.ResponseStatusCode |
                HttpLoggingFields.Duration;

            // Add selective request headers (no sensitive data)
            options.RequestHeaders.Add("User-Agent");
            options.RequestHeaders.Add("X-Correlation-Id");
            options.RequestHeaders.Add("Accept");
            options.RequestHeaders.Add("Content-Type");

            // Add selective response headers
            options.ResponseHeaders.Add("Content-Type");
            options.ResponseHeaders.Add("X-Correlation-Id");

            // Exclude sensitive paths
            options.CombineLogs = true;
        });

        return services;
    }
}
