// -----------------------------------------------------------------------
// <copyright file="SecurityEventType.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Domain.Enums;

public static class SecurityEventType
{
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string TokenRefreshed = "TokenRefreshed";
    public const string TokenRevoked = "TokenRevoked";
    public const string TokenReuseDetected = "TokenReuseDetected";
    public const string AllTokensRevoked = "AllTokensRevoked";
    public const string AccountLocked = "AccountLocked";
    public const string AccountUnlocked = "AccountUnlocked";
    public const string PasswordChanged = "PasswordChanged";
    public const string EmailConfirmed = "EmailConfirmed";
    public const string RoleChanged = "RoleChanged";
    public const string MfaEnabled = "MfaEnabled";
    public const string MfaDisabled = "MfaDisabled";
    public const string MfaVerified = "MfaVerified";
    public const string MfaFailed = "MfaFailed";
}
