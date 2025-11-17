// -----------------------------------------------------------------------
// <copyright file="LoginCommandValidator.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using Acme.Application.Shared.Validators;

namespace Acme.Application.Features.Account.Login;

/// <summary>
/// Validator for login command.
/// Note: For login we only validate format, not password complexity.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).EmailMustBeValid();
        RuleFor(x => x.Password).PasswordMustNotBeEmpty();
    }
}
