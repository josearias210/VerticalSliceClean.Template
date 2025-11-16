// -----------------------------------------------------------------------
// <copyright file="ProductService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Features.Account.RefreshToken;

using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class RefreshTokenCommandHandler(
    ITokenService tokenService,
    UserManager<Account> userManager,
    ICookieTokenService cookieTokenService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, ErrorOr<RefreshTokenCommandResponse>>
{
    private readonly ITokenService tokenService = tokenService;
    private readonly UserManager<Account> userManager = userManager;
    private readonly ICookieTokenService cookieTokenService = cookieTokenService;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly ILogger<RefreshTokenCommandHandler> logger = logger;

    public async Task<ErrorOr<RefreshTokenCommandResponse>> Handle(RefreshTokenCommand refreshTokenCommand, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to refresh token");
        
        // Get refresh token from cookie
        var refreshToken = cookieTokenService.GetRefreshToken();
        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("Refresh token not found in cookie");
            return Error.Unauthorized("Auth.MissingRefreshToken", "Refresh token not found");
        }

        // Get IP and UserAgent for audit
        var context = httpContextAccessor.HttpContext;
        var ip = context?.Connection.RemoteIpAddress?.ToString();
        var userAgent = context?.Request.Headers["User-Agent"].ToString();
        
        var result = await tokenService.RefreshAsync(refreshToken, ip, userAgent);
        if (result.IsError)
        {
            logger.LogWarning("Token refresh failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return result.Errors;
        }

        // Set new tokens as httpOnly cookies
        cookieTokenService.SetTokenCookies(result.Value.accessToken, result.Value.refreshToken);

        // Get user info to return
        var userId = context?.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            // Extract from new access token if not in current context
            userId = ExtractUserIdFromToken(result.Value.accessToken);
        }

        var account = await userManager.FindByIdAsync(userId!);
        if (account == null)
        {
            logger.LogError("User not found after successful token refresh: {UserId}", userId);
            return Error.NotFound("Auth.UserNotFound", "User not found");
        }

        var roles = await userManager.GetRolesAsync(account);

        logger.LogInformation("Token refreshed successfully for user {UserId}", userId);
        return new RefreshTokenCommandResponse 
        { 
            UserId = account.Id,
            Email = account.Email ?? string.Empty,
            FullName = account.FullName,
            Roles = roles,
            EmailConfirmed = account.EmailConfirmed
        };
    }

    private static string ExtractUserIdFromToken(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;
    }
}
