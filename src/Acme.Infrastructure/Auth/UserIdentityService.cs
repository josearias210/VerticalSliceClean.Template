namespace Acme.Infrastructure.Auth;

using Acme.Application.Abstractions;
using Microsoft.AspNetCore.Http;

public class UserIdentityService(IHttpContextAccessor httpContextAccessor) : IUserIdentityService
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
    public string? UserName => httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;
    public string GetRole() => httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value ?? throw new InvalidOperationException("User roles missing.");
}
