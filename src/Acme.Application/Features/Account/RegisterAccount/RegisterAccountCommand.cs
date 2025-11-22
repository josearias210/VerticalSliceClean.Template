namespace Acme.Application.Features.Account.RegisterAccount;

using Acme.Domain.Enums;
using ErrorOr;
using MediatR;

public class RegisterAccountCommand : IRequest<ErrorOr<Unit>>
{
    public required string FirstName { get; set; }
    public required string Email { get; set; }
    public required Role Role { get; set; }
}
