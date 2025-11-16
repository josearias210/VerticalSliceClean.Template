// -----------------------------------------------------------------------
// <copyright file="ITokenGenerator.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Acme.Domain.Entities;

namespace Acme.Application.Abstractions;

public interface ITokenGenerator
{
    Task<string> GenerateAccessTokenAsync(Account account, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashToken(string token);
}
