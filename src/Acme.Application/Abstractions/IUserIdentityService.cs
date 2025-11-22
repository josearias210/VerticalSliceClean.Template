namespace Acme.Application.Abstractions;

public interface IUserIdentityService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
