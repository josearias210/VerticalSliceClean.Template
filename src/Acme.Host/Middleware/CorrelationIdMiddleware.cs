using System.Diagnostics;

namespace Acme.Host.Middleware;

/// <summary>
/// Middleware that generates or extracts correlation IDs for request tracing.
/// Links correlation ID to OpenTelemetry Activity for distributed tracing.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var cid) || string.IsNullOrWhiteSpace(cid))
        {
            cid = Guid.NewGuid().ToString();
        }

        var correlationId = cid.ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // Link correlation ID to OpenTelemetry Activity
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("correlation.id", correlationId);
        }

        await next(context);
    }
}
