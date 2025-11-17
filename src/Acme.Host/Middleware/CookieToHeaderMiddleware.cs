// -----------------------------------------------------------------------
// <copyright file="CookieToHeaderMiddleware.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Host.Middleware;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Middleware that extracts JWT from httpOnly cookie and adds it to Authorization header
/// This allows the standard JWT Bearer authentication to work with cookies
/// </summary>
public class CookieToHeaderMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;
    private const string AccessTokenCookieName = "accessToken";

    public async Task InvokeAsync(HttpContext context)
    {
        // If Authorization header is already present, don't override it
        // Extract token from cookie and validate it's not empty
        if (!context.Request.Headers.ContainsKey("Authorization") &&
            context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var token) &&
            !string.IsNullOrWhiteSpace(token))
        {
            // Add token to Authorization header for JWT authentication middleware
            context.Request.Headers.Append("Authorization", $"Bearer {token}");
        }

        await next(context);
    }
}
