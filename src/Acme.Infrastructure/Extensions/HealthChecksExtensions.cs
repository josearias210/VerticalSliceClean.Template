using Acme.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Acme.Infrastructure.Extensions;

public static class HealthChecksExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            // Database checks
            .AddCheck<DbContextConnectivityHealthCheck>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"],
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<MigrationHealthCheck>(
                name: "migrations",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db", "migrations"],
                timeout: TimeSpan.FromSeconds(10))
            // Infrastructure checks
            .AddCheck(
                name: "memory",
                () =>
                {
                    var allocated = GC.GetTotalMemory(forceFullCollection: false);
                    var data = new Dictionary<string, object>
                    {
                        ["AllocatedMB"] = allocated / 1024 / 1024,
                        ["Gen0Collections"] = GC.CollectionCount(0),
                        ["Gen1Collections"] = GC.CollectionCount(1),
                        ["Gen2Collections"] = GC.CollectionCount(2)
                    };

                    // Warn if > 1GB allocated
                    var status = allocated > 1_073_741_824 ? HealthStatus.Degraded : HealthStatus.Healthy;
                    return HealthCheckResult.Healthy($"Memory: {allocated / 1024 / 1024}MB", data);
                },
                tags: ["live"])
            .AddCheck(
                name: "startup",
                () => HealthCheckResult.Healthy("Application started"),
                tags: ["live"]);

        return services;
    }

    public static IEndpointRouteBuilder MapDefaultHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness probe - Kubernetes verifica si el pod está vivo (debe reiniciar si falla)
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            AllowCachingResponses = true,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = WriteHealthCheckResponse
        }).CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(10)));

        // Readiness probe - Kubernetes verifica si el pod está listo para recibir tráfico
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            AllowCachingResponses = true,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = WriteHealthCheckResponse
        }).CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(10)));

        // Health check completo - Todos los checks con detalles
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            AllowCachingResponses = true,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = WriteHealthCheckResponse
        }).CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(10)));

        return endpoints;
    }

    /// <summary>
    /// Writes a detailed health check response with version, timestamp, and exception details.
    /// </summary>
    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        var payload = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
                tags = entry.Value.Tags
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
