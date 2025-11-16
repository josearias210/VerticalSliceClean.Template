using Microsoft.Extensions.DependencyInjection;

namespace Acme.Api.Extensions;

public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Configures ProblemDetails with .NET 10 best practices:
    /// - Automatic traceId and correlationId injection
    /// - Custom error code support
    /// - Consistent formatting
    /// </summary>
    public static IServiceCollection AddCustomizedProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            // Customize all ProblemDetails responses (both from IExceptionHandler and Results.Problem)
            options.CustomizeProblemDetails = ctx =>
            {
                var http = ctx.HttpContext;
                
                // Always add traceId for correlation with logs
                ctx.ProblemDetails.Extensions["traceId"] = http.TraceIdentifier;
                
                // Add correlationId if available (from CorrelationIdMiddleware)
                if (http.Items.TryGetValue("CorrelationId", out var cid) && cid is string correlationId)
                {
                    ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
                }

                // Set default type if not specified
                ctx.ProblemDetails.Type ??= $"https://httpstatuses.com/{ctx.ProblemDetails.Status ?? 500}";
            };
        });

        return services;
    }
}
