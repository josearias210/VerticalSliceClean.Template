namespace Acme.Domain.Extensions;

using Acme.Domain.Constants;
using Acme.Domain.Enums;

/// <summary>
/// Extension methods for Role enum.
/// </summary>
public static class RoleExtensions
{
    /// <summary>
    /// Converts Role enum to its string representation.
    /// Avoids using ToString() everywhere.
    /// </summary>
    public static string ToRoleName(this Role role)
    {
        return role switch
        {
            Role.Developer => Roles.Developer,
            Role.Admin => Roles.Admin,
            Role.User => Roles.User,
            Role.Manager => Roles.Manager,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
        };
    }

    /// <summary>
    /// Converts string to Role enum.
    /// </summary>
    public static Role ToRole(this string roleName)
    {
        return roleName switch
        {
            Roles.Developer => Role.Developer,
            Roles.Admin => Role.Admin,
            Roles.User => Role.User,
            Roles.Manager => Role.Manager,
            _ => throw new ArgumentException($"Invalid role name: {roleName}", nameof(roleName))
        };
    }

    /// <summary>
    /// Tries to convert string to Role enum.
    /// Returns true if successful, false otherwise.
    /// </summary>
    public static bool TryToRole(this string roleName, out Role role)
    {
        role = roleName switch
        {
            Roles.Developer => Role.Developer,
            Roles.Admin => Role.Admin,
            Roles.User => Role.User,
            Roles.Manager => Role.Manager,
            _ => default
        };

        return roleName is Roles.Developer or Roles.Admin or Roles.User or Roles.Manager;
    }
}
