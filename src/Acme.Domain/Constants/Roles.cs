namespace Acme.Domain.Constants;

/// <summary>
/// Role names as constants to avoid magic strings.
/// These match the Role enum values.
/// </summary>
public static class Roles
{
    public const string Developer = nameof(Developer);
    public const string Admin = nameof(Admin);
    public const string User = nameof(User);
    public const string Manager = nameof(Manager);
}
