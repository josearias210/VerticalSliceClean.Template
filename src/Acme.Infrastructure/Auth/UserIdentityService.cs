namespace Acme.Infrastructure.Auth;

using Acme.Application.Abstractions;
using Microsoft.AspNetCore.Http;

public class UserIdentityService(IHttpContextAccessor httpContextAccessor) : IUserIdentityService
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value; // o ClaimTypes.NameIdentifier

    public string? UserName => httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value; // o ClaimTypes.Name

    public string GetRole() => httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value ?? throw new InvalidOperationException("User roles missing.");

    public IEnumerable<string> GetScopes()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return [];
        }

        var scopeClaims = user.FindAll("scope").ToList();

        if (scopeClaims.Count == 0)
        {
            return [];
        }

        var allScopes = scopeClaims
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        return allScopes;
    }

    public bool HasScope(string scope) => GetScopes().Contains(scope, StringComparer.OrdinalIgnoreCase);
    
}
