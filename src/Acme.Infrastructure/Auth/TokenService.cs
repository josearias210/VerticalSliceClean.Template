// -----------------------------------------------------------------------
// <copyright file="TokenService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Infrastructure.Auth;

using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using Acme.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TokenService(
    ITokenGenerator tokenGenerator,
    IApplicationDbContext dbContext,
    UserManager<Account> userManager,
    IOptions<JwtSettings> jwtOptions,
    ILogger<TokenService> logger) : ITokenService
{
    private readonly ITokenGenerator tokenGenerator = tokenGenerator;
    private readonly IApplicationDbContext dbContext = dbContext;
    private readonly UserManager<Account> userManager = userManager;
    private readonly JwtSettings jwtSettings = jwtOptions.Value;
    private readonly ILogger<TokenService> logger = logger;

    public async Task<ErrorOr<(string accessToken, string refreshToken)>> CreateTokensAsync(Account account)
    {
        var roles = await userManager.GetRolesAsync(account);
        var accessToken = await tokenGenerator.GenerateAccessTokenAsync(account, roles);
        var refreshPlain = tokenGenerator.GenerateRefreshToken();
        var refreshHash = tokenGenerator.HashToken(refreshPlain);

        var entity = new RefreshToken
        {
            UserId = account.Id,
            TokenHash = refreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenDays),
        };

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Tokens created successfully for user {UserId}", account.Id);
        return (accessToken, refreshPlain);
    }

    public async Task<ErrorOr<(string accessToken, string refreshToken)>> RefreshAsync(string refreshTokenPlain, string? ip = null, string? userAgent = null)
    {
        var hash = tokenGenerator.HashToken(refreshTokenPlain);
        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);
        
        if (token is null)
        {
            logger.LogWarning("Refresh attempt with invalid token");
            return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid refresh token");
        }

        if (token.IsExpired)
        {
            logger.LogWarning("Refresh attempt with expired token for user {UserId}", token.UserId);
            return Error.Unauthorized("Auth.ExpiredRefreshToken", "Expired refresh token");
        }

        if (token.RevokedAt is not null)
        {
            // TOKEN REUSE DETECTED - Possible token theft!
            logger.LogError(
                "SECURITY ALERT: Revoked token reused for user {UserId}. Token created: {CreatedAt}. Revoking all tokens.", 
                token.UserId, token.CreatedAt);
            
            // Revoke ALL active tokens for this user as security measure
            var allActiveTokens = await dbContext.RefreshTokens
                .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
                .ToListAsync();
            
            foreach (var activeToken in allActiveTokens)
            {
                activeToken.RevokedAt = DateTime.UtcNow;
            }
            
            await dbContext.SaveChangesAsync();
            
            return Error.Unauthorized(
                "Auth.TokenReuseDetected", 
                "Security violation detected. All sessions have been terminated. Please login again.");
        }

        var user = await userManager.FindByIdAsync(token.UserId);
        if (user is null)
        {
            logger.LogError("User {UserId} not found during token refresh", token.UserId);
            return Error.NotFound("Auth.UserNotFound", "User not found");
        }

        // Rotate: revoke current & create new
        token.RevokedAt = DateTime.UtcNow;
        var newPlain = tokenGenerator.GenerateRefreshToken();
        var newHash = tokenGenerator.HashToken(newPlain);
        token.ReplacedByTokenHash = newHash;

        // persist revoke
        await dbContext.SaveChangesAsync();

        var newEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenDays),
            IpAddress = ip,
            UserAgent = userAgent,
        };

        dbContext.RefreshTokens.Add(newEntity);
        await dbContext.SaveChangesAsync();

        var roles = await userManager.GetRolesAsync(user);
        var newAccess = await tokenGenerator.GenerateAccessTokenAsync(user, roles);

        logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);
        return (newAccess, newPlain);
    }

    public async Task<ErrorOr<bool>> RevokeAsync(string refreshTokenPlain, CancellationToken ct = default)
    {
        var hash = tokenGenerator.HashToken(refreshTokenPlain);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);
        
        if (token is null)
        {
            logger.LogWarning("Revoke attempt with invalid token");
            return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid refresh token");
        }

        if (token.RevokedAt is not null)
        {
            logger.LogDebug("Token already revoked for user {UserId}", token.UserId);
            return true;
        }

        token.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Token revoked successfully for user {UserId}", token.UserId);
        return true;
    }
}
