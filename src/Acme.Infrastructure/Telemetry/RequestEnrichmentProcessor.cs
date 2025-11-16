using Microsoft.AspNetCore.Http;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Acme.Infrastructure.Telemetry;

public class RequestEnrichmentProcessor(IHttpContextAccessor httpContextAccessor) : BaseProcessor<LogRecord>
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public override void OnEnd(LogRecord logRecord)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null)
            return;

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("http.path", context.Request.Path.ToString()),
            new("http.method", context.Request.Method),
            new("http.scheme", context.Request.Scheme)
        };

        // Add user ID if authenticated
        var userId = context.User?.FindFirst("sub")?.Value 
                     ?? context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            attributes.Add(new("user.id", userId));
        }

        // Add user email if available
        var userEmail = context.User?.FindFirst("email")?.Value
                        ?? context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (!string.IsNullOrEmpty(userEmail))
        {
            attributes.Add(new("user.email", userEmail));
        }

        // Add user name if available
        var userName = context.User?.FindFirst("name")?.Value;
        if (!string.IsNullOrEmpty(userName))
        {
            attributes.Add(new("user.name", userName));
        }

        // Add preferred username if available
        var preferredUsername = context.User?.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrEmpty(preferredUsername))
        {
            attributes.Add(new("user.preferred_username", preferredUsername));
        }

        // Add email verified status
        var emailVerified = context.User?.FindFirst("email_verified")?.Value;
        if (!string.IsNullOrEmpty(emailVerified))
        {
            attributes.Add(new("user.email_verified", emailVerified));
        }

        // Add user roles
        var roles = context.User?.FindAll("role").Select(c => c.Value).ToList();
        if (roles != null && roles.Count > 0)
        {
            attributes.Add(new("user.roles", string.Join(",", roles)));
        }

        // Add correlation ID if present
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
        {
            attributes.Add(new("correlation.id", correlationId.ToString()));
        }

        // Add client IP
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            attributes.Add(new("client.ip", clientIp));
        }

        // Add user agent
        if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            attributes.Add(new("user.agent", userAgent.ToString()));
        }

        // Merge with existing attributes
        if (logRecord.Attributes != null)
        {
            foreach (var attr in logRecord.Attributes)
            {
                attributes.Add(attr);
            }
        }

        logRecord.Attributes = attributes;
    }
}
