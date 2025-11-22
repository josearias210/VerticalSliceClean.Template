namespace Acme.Application.Features.Account.RegisterAccount;

using FluentValidation;
using static Acme.Application.Common.ErrorCodes;

public class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithErrorCode(Account.FirstNameEmpty);
        RuleFor(x => x.Email).NotEmpty().WithErrorCode(Account.EmailEmpty);
        RuleFor(x => x.Role).NotEmpty().WithErrorCode(Account.RoleEmpty);
        RuleFor(x => x.Role).IsInEnum().WithErrorCode(Account.RoleInvalid);
    }
}
