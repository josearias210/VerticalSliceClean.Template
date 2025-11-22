namespace Acme.Application.Features.Account.RegisterAccount;

using Acme.Application.Abstractions;
using Acme.Application.Common;
using Acme.Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class RegisterAccountCommandHandler(UserManager<Account> userManager, IEmailService emailService, IPasswordGenerator passwordGenerator, ILogger<RegisterAccountCommandHandler> logger) : IRequestHandler<RegisterAccountCommand, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering new account for {Email}", command.Email);

        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is not null)
        {
            logger.LogWarning("The {Email} is already in use", command.Email);
            return Error.Conflict(ErrorCodes.Account.EmailExists);
        }

        var temporaryPassword = passwordGenerator.GenerateStrong(16);

        var account = new Account
        {
            FullName = command.FirstName,
            Email = command.Email,
            UserName = command.Email,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(account, temporaryPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Account creation failed for {Email}: {Errors}", command.Email, string.Join(", ", createResult.Errors.Select(e => e.Code)));
            return Error.Failure(ErrorCodes.Account.CreateFailed);
        }

        var roleResult = await userManager.AddToRoleAsync(account, command.Role.ToString());
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to add role {Role} to account {Email}: {Errors}", temporaryPassword, command.Email, string.Join(", ", roleResult.Errors.Select(e => e.Code)));

            // Compensation: Delete the user to avoid inconsistent state
            await userManager.DeleteAsync(account);
            
            return Error.Failure(ErrorCodes.Account.RoleAssignFailed);
        }

        try
        {
            await emailService.SendWelcomeWithPasswordAsync(command.Email, temporaryPassword, cancellationToken);
            // SECURITY: Do not log the password!
            logger.LogInformation("Account registered successfully for {Email} with role {Role}. Welcome email sent.", command.Email, command.Role);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", command.Email);
        }

        logger.LogInformation("ending new account for {Email}", command.Email);

        return Unit.Value;
    }
}
