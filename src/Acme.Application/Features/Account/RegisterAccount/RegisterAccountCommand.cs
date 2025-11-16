// -----------------------------------------------------------------------
// <copyright file="RegisterUserRequest.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ErrorOr;
using MediatR;

namespace Acme.Application.Features.Account.RegisterAccount;

/// <summary>
/// Command to register a new user account.
/// Password field is used to specify the role (e.g., "Admin", "User").
/// Actual password is generated randomly and sent via email.
/// </summary>
public class RegisterAccountCommand : IRequest<ErrorOr<Unit>>
{
    public required string Email { get; set; }
    
    /// <summary>
    /// Role to assign to the user (e.g., "Admin", "User", "Manager").
    /// </summary>
    public required string Password { get; set; } // Repurposed as Role for this implementation
}
