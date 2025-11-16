using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Acme.AppHost.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddConfiguredRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Policy for authentication endpoints (more restrictive)
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(15);
                opt.PermitLimit = 5;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0; // No queue, reject immediately
            });

            // More permissive global policy
            options.AddFixedWindowLimiter("general", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 100;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            // Per-IP policy to prevent abuse
            options.AddConcurrencyLimiter("per-ip", opt =>
            {
                opt.PermitLimit = 10;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });

            options.RejectionStatusCode = 429; // Too Many Requests

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    await context.HttpContext.Response.WriteAsync(
                        $"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds.",
                        token);
                }
                else
                {
                    await context.HttpContext.Response.WriteAsync(
                        "Too many requests. Please try again later.",
                        token);
                }
            };
        });

        return services;
    }
}
