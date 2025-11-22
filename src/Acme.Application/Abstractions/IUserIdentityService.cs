namespace Acme.Application.Abstractions;

public interface IUserIdentityService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    string? GetRole();
    
    /// <summary>
    /// Gets all scopes from the JWT token.
    /// </summary>
    IEnumerable<string> GetScopes();
    
    /// <summary>
    /// Checks if the user has a specific scope.
    /// </summary>
    bool HasScope(string scope);
}
