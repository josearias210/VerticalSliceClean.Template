namespace Acme.Infrastructure.Auth;

using Acme.Application.Abstractions;
using Microsoft.AspNetCore.Http;

public class UserIdentityService(IHttpContextAccessor httpContextAccessor) : IUserIdentityService
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value; // o ClaimTypes.NameIdentifier

    public string? UserName => httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value; // o ClaimTypes.Name
}
