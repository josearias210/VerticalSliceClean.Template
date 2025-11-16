// -----------------------------------------------------------------------
// <copyright file="ICookieTokenService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Abstractions;

/// <summary>
/// Service for managing JWT tokens in httpOnly cookies
/// </summary>
public interface ICookieTokenService
{
    /// <summary>
    /// Sets access and refresh tokens as httpOnly cookies
    /// </summary>
    void SetTokenCookies(string accessToken, string refreshToken);

    /// <summary>
    /// Gets the access token from cookie
    /// </summary>
    string? GetAccessToken();

    /// <summary>
    /// Gets the refresh token from cookie
    /// </summary>
    string? GetRefreshToken();

    /// <summary>
    /// Clears all auth cookies (logout)
    /// </summary>
    void ClearTokenCookies();
}
