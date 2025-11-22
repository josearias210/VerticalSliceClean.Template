namespace Acme.Infrastructure.Auth;

using Acme.Application.Abstractions;
using Microsoft.AspNetCore.Http;

public class UserIdentityService(IHttpContextAccessor httpContextAccessor) : IUserIdentityService
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value; // o ClaimTypes.NameIdentifier

    public string? UserName => httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value; // o ClaimTypes.Name

    public string? GetRole()
    {
        // OpenIddict usa "role" como claim type
        // Como cada usuario tiene un solo rol, tomamos el primero
        return httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;
    }

    public IEnumerable<string> GetScopes()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null) return Enumerable.Empty<string>();

        // OpenIddict usa "scope" como claim type
        // Los scopes pueden venir como múltiples claims o como un solo claim separado por espacios
        var scopeClaims = user.FindAll("scope").ToList();
        
        if (!scopeClaims.Any())
            return Enumerable.Empty<string>();

        // Si hay múltiples claims de scope, combinarlos
        var allScopes = scopeClaims
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        return allScopes;
    }

    public bool HasScope(string scope)
    {
        return GetScopes().Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}
