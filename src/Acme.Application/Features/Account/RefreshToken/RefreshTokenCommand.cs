// -----------------------------------------------------------------------
// <copyright file="RefreshTokenRequest.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ErrorOr;
using MediatR;

namespace Acme.Application.Features.Account.RefreshToken;

/// <summary>
/// Refresh token command - token is extracted from httpOnly cookie
/// </summary>
public class RefreshTokenCommand : IRequest<ErrorOr<RefreshTokenCommandResponse>>  
{
    // No properties needed - token comes from cookie
}
