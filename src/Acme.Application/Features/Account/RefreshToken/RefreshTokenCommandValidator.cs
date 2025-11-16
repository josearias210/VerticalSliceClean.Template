using FluentValidation;

namespace Acme.Application.Features.Account.RefreshToken;
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        // No validation needed since token comes from httpOnly cookie
        // Cookie validation is handled by the middleware
    }
}
