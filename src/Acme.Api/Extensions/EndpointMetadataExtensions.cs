using Microsoft.AspNetCore.Builder;

namespace Acme.Api.Extensions;

/// <summary>
/// Extension methods to reduce endpoint metadata verbosity.
/// Groups common patterns for authentication and documentation.
/// </summary>
public static class EndpointMetadataExtensions
{
    /// <summary>
    /// Configures an anonymous authentication endpoint with rate limiting.
    /// Common for login, refresh, register endpoints.
    /// </summary>
    public static RouteHandlerBuilder WithAnonymousAuth(
        this RouteHandlerBuilder builder,
        string rateLimitPolicy = "auth")
    {
        return builder
            .AllowAnonymous()
            .RequireRateLimiting(rateLimitPolicy);
    }

    /// <summary>
    /// Adds complete endpoint metadata: name, summary, and description.
    /// </summary>
    public static RouteHandlerBuilder WithMetadata(
        this RouteHandlerBuilder builder,
        string name,
        string summary)
    {
        return builder
            .WithName(name)
            .WithDisplayName(summary);
    }
}
