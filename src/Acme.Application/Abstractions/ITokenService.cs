// -----------------------------------------------------------------------
// <copyright file="TokenService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ErrorOr;
using Acme.Domain.Entities;

namespace Acme.Application.Abstractions;

public interface ITokenService
{
    Task<ErrorOr<(string accessToken, string refreshToken)>> CreateTokensAsync(Account account);
    Task<ErrorOr<(string accessToken, string refreshToken)>> RefreshAsync(string refreshTokenPlain, string? ip = null, string? userAgent = null);
    Task<ErrorOr<bool>> RevokeAsync(string refreshTokenPlain, CancellationToken ct = default);
}
