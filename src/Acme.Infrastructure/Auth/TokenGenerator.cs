// -----------------------------------------------------------------------
// <copyright file="TokenGenerator.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using Acme.Infrastructure.Auth.Models;
using Acme.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Acme.Infrastructure.Auth;

public class TokenGenerator(IOptions<JwtSettings> jwtOptions) : ITokenGenerator
{
    private readonly JwtSettings jwtSettings = jwtOptions.Value;

    public Task<string> GenerateAccessTokenAsync(Account account, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(AcmeClaim.UserId, account.Id),
            new(AcmeClaim.Email, account.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, account.Id),
        };

        // Enrich with additional claims
        if (!string.IsNullOrEmpty(account.FullName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, account.FullName));
        }

        if (!string.IsNullOrEmpty(account.PreferredUsername))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.PreferredUsername, account.PreferredUsername));
        }

        if (account.EmailConfirmed)
        {
            claims.Add(new Claim("email_verified", "true"));
        }

        claims.AddRange(roles.Select(role => new Claim(AcmeClaim.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenMinutes),
            signingCredentials: creds
        );

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
