using ErrorOr;
using MediatR;

namespace Acme.Application.Features.Account.Login;

public class LoginCommand : IRequest<ErrorOr<LoginCommandResponse>>
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
