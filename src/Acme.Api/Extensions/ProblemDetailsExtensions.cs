namespace Acme.Api.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddCustomizedProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                var http = ctx.HttpContext;
                ctx.ProblemDetails.Extensions["traceId"] = http.TraceIdentifier;
                if (http.Items.TryGetValue("CorrelationId", out var cid) && cid is string correlationId)
                {
                    ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
                ctx.ProblemDetails.Type ??= $"https://httpstatuses.com/{ctx.ProblemDetails.Status ?? 500}";
            };
        });
        return services;
    }
}
