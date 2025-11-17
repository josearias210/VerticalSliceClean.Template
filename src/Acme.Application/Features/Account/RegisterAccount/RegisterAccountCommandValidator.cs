using FluentValidation;
using Acme.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using AccountEntity = Acme.Domain.Entities.Account;
using Acme.Application.Shared.Validators;

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
        RuleFor(x => x.Email)
            .EmailMustBeValid()
            .MustAsync(async (email, cancellationToken) =>
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                return existingUser == null;
            })
            .WithMessage("Email is already registered")
            .WithErrorCode("Email.AlreadyExists");

        // Password field is repurposed as Role
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Role is required")
            .WithErrorCode("Role.Required")
            .Must(role => role == Role.Admin.ToString() || 
                          role == Role.User.ToString() || 
                          role == Role.Manager.ToString() || 
                          role == Role.Developer.ToString())
            .WithMessage($"Invalid role. Valid roles: {Role.Admin}, {Role.User}, {Role.Manager}, {Role.Developer}")
            .WithErrorCode("Role.Invalid");
    }
}
