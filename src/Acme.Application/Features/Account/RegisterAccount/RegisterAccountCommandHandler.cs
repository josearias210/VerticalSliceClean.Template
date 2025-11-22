namespace Acme.Application.Features.Account.RegisterAccount;

using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class RegisterAccountCommandHandler(UserManager<Account> userManager, IEmailService emailService, IPasswordGenerator passwordGenerator, ILogger<RegisterAccountCommandHandler> logger) : IRequestHandler<RegisterAccountCommand, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(RegisterAccountCommand registerAccountCommand, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering new account for {Email}", registerAccountCommand.Email);

        var temporaryPassword = passwordGenerator.GenerateStrong(16);

        var account = new Account
        {
            EmailConfirmed = false,
            Email = registerAccountCommand.Email,
            UserName = registerAccountCommand.Email,
        };

        var createResult = await userManager.CreateAsync(account, temporaryPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Account creation failed for {Email}: {Errors}", registerAccountCommand.Email, string.Join(", ", createResult.Errors.Select(e => e.Code)));
            return Error.Failure("Account.CreateFailed", "Failed to create account");
        }

        var roleResult = await userManager.AddToRoleAsync(account, registerAccountCommand.Password);
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to add role {Role} to account {Email}: {Errors}", registerAccountCommand.Password, registerAccountCommand.Email, string.Join(", ", roleResult.Errors.Select(e => e.Code)));
            return Error.Failure("Account.RoleAssignFailed", "Failed to assign role");
        }

        try
        {
            await emailService.SendWelcomeWithPasswordAsync(registerAccountCommand.Email, temporaryPassword, cancellationToken);
            logger.LogInformation("Account registered successfully for {Email} with role {Role}. Welcome email sent.", registerAccountCommand.Email, registerAccountCommand.Password);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", registerAccountCommand.Email);
        }

        return Unit.Value;
    }
}
