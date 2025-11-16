namespace Acme.Application.Abstractions;

public interface IUserIdentityService
{
    string? GetEmail();
    string? GetRole();
    string? GetUserId();
}