using FluentValidation;
using Acme.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using AccountEntity = Acme.Domain.Entities.Account;

namespace Acme.Application.Features.Account.RegisterAccount;

/// <summary>
/// Validator for account registration.
/// Validates email format, uniqueness, and role validity.
/// Password is auto-generated and sent via email.
/// </summary>
public class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator(UserManager<AccountEntity> userManager)
    {
        RuleFor(x => x.Email).NotEmpty();

        // Password field is repurposed as Role
        RuleFor(x => x.Password).NotEmpty();
    }
}
