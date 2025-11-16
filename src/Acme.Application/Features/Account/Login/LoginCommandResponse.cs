// -----------------------------------------------------------------------
// <copyright file="LoginResponse.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Features.Account.Login;

public class LoginCommandResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
    public required IEnumerable<string> Roles { get; set; }
    public bool EmailConfirmed { get; set; }
}
