using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Acme.Api.Extensions;

/// <summary>
/// Extensions for API versioning support using Asp.Versioning library.
/// Enables semantic versioning with backward compatibility.
/// </summary>
public static class ApiVersioningExtensions
{
    private static readonly char[] ResourceNameSeparators = ['-', '_'];

    /// <summary>
    /// Creates a versioned API group using native ASP.NET API versioning.
    /// Example: MapVersionedGroup(v1, "accounts") → /api/v1/accounts
    /// </summary>
    public static RouteGroupBuilder MapVersionedGroup(
        this IEndpointRouteBuilder app,
        ApiVersion version,
        string resource)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(version)
            .ReportApiVersions()
            .Build();

        return app.MapGroup($"/api/v{version.MajorVersion}/{resource}")
            .WithApiVersionSet(versionSet)
            .WithTags(ToPascalCase(resource))
            .WithOpenApi();
    }

    /// <summary>
    /// Creates a versioned API group for v1 (current default).
    /// Uses native API versioning for future compatibility.
    /// </summary>
    public static RouteGroupBuilder MapV1Group(
        this IEndpointRouteBuilder app,
        string resource)
    {
        return app.MapVersionedGroup(new ApiVersion(1, 0), resource);
    }

    /// <summary>
    /// Creates a versioned API group for v2 (future version).
    /// Ready for when breaking changes are needed.
    /// </summary>
    public static RouteGroupBuilder MapV2Group(
        this IEndpointRouteBuilder app,
        string resource)
    {
        return app.MapVersionedGroup(new ApiVersion(2, 0), resource);
    }

    /// <summary>
    /// Converts resource name to PascalCase for tag naming.
    /// Example: "accounts" → "Accounts", "user-profiles" → "UserProfiles"
    /// </summary>
    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var parts = value.Split(ResourceNameSeparators, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => 
            char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
    }
}
