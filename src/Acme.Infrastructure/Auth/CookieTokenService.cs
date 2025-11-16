// -----------------------------------------------------------------------
// <copyright file="CookieTokenService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Infrastructure.Auth;

using Acme.Application.Abstractions;
using Acme.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

/// <summary>
/// Service for managing JWT tokens in httpOnly cookies
/// </summary>
public class CookieTokenService(IOptions<JwtSettings> jwtOptions, IHttpContextAccessor httpContextAccessor) : ICookieTokenService
{
    private readonly JwtSettings jwtSettings = jwtOptions.Value;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    private const string AccessTokenCookieName = "accessToken";
    private const string RefreshTokenCookieName = "refreshToken";

    /// <summary>
    /// Sets access and refresh tokens as httpOnly cookies
    /// </summary>
    public void SetTokenCookies(string accessToken, string refreshToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return;

        var isSecure = context.Request.IsHttps;
        var sameSiteMode = GetSameSiteMode(context);

        // Access Token Cookie (short-lived)
        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSiteMode,
            MaxAge = TimeSpan.FromMinutes(jwtSettings.AccessTokenMinutes),
            Path = "/",
            IsEssential = true
        };

        context.Response.Cookies.Append(AccessTokenCookieName, accessToken, accessCookieOptions);

        // Refresh Token Cookie (long-lived)
        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSiteMode,
            MaxAge = TimeSpan.FromDays(jwtSettings.RefreshTokenDays),
            Path = "/api/v1/accounts/refresh", // Only sent to refresh endpoint
            IsEssential = true
        };

        context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, refreshCookieOptions);
    }

    /// <summary>
    /// Gets the access token from cookie
    /// </summary>
    public string? GetAccessToken()
    {
        var context = httpContextAccessor.HttpContext;
        return context?.Request.Cookies[AccessTokenCookieName];
    }

    /// <summary>
    /// Gets the refresh token from cookie
    /// </summary>
    public string? GetRefreshToken()
    {
        var context = httpContextAccessor.HttpContext;
        return context?.Request.Cookies[RefreshTokenCookieName];
    }

    /// <summary>
    /// Clears all auth cookies (logout)
    /// </summary>
    public void ClearTokenCookies()
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = GetSameSiteMode(context),
            MaxAge = TimeSpan.FromSeconds(-1), // Expire immediately
            Path = "/"
        };

        context.Response.Cookies.Delete(AccessTokenCookieName, cookieOptions);
        
        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = GetSameSiteMode(context),
            MaxAge = TimeSpan.FromSeconds(-1),
            Path = "/api/v1/accounts/refresh"
        };
        
        context.Response.Cookies.Delete(RefreshTokenCookieName, refreshOptions);
    }

    private static SameSiteMode GetSameSiteMode(HttpContext context)
    {
        // For localhost development, use Lax to allow cross-origin requests
        // For production, use Strict for maximum security
        var origin = context.Request.Headers["Origin"].FirstOrDefault();
        var isLocalhost = origin?.Contains("localhost") ?? false;
        
        return isLocalhost ? SameSiteMode.Lax : SameSiteMode.Strict;
    }
}
