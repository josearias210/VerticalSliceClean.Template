namespace Acme.Application.Features.Account.RegisterAccount;

using Acme.Application.Abstractions;
using Acme.Application.Common;
using Acme.Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class RegisterAccountCommandHandler(
    UserManager<Account> userManager, 
    IEmailService emailService, 
    IPasswordGenerator passwordGenerator, 
    IUserIdentityService userIdentityService,
    ILogger<RegisterAccountCommandHandler> logger) : IRequestHandler<RegisterAccountCommand, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registering new account for {Email}", command.Email);

        // Permission validation
        var permissionError = ValidateUserPermissions(command.Role);
        if (permissionError != null)
        {
            return permissionError.Value;
        }

        // EJEMPLO: Validación adicional por scope (comentado, no se ejecuta)
        // Si quieres validar por scope en lugar de por rol, puedes usar:
        /*
        var scopeError = ValidateUserPermissionsByScope(command.Role);
        if (scopeError != null)
        {
            return scopeError.Value;
        }
        */

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
            logger.LogError("Failed to add role {Role} to account {Email}: {Errors}", command.Role, command.Email, string.Join(", ", roleResult.Errors.Select(e => e.Code)));

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

    /// <summary>
    /// Validates if the current user has permission to create an account with the specified role.
    /// - Developer: Can create any role
    /// - Admin: Can create any role except Admin
    /// - Others: Cannot create accounts
    /// </summary>
    private ErrorOr<Unit>? ValidateUserPermissions(Domain.Enums.Role targetRole)
    {
        if (!userIdentityService.IsAuthenticated)
        {
            return null; // Allow unauthenticated requests (e.g., initial setup)
        }

        var currentUserRole = userIdentityService.GetRole();
        
        if (string.IsNullOrEmpty(currentUserRole))
        {
            logger.LogWarning("User {UserId} has no role assigned", userIdentityService.UserId);
            return Error.Forbidden(ErrorCodes.Account.InsufficientPermissions);
        }

        // Developer can create any role
        if (currentUserRole == Domain.Enums.Role.Developer.ToString())
        {
            return null; // Permission granted
        }

        // Admin can create any role except Admin
        if (currentUserRole == Domain.Enums.Role.Admin.ToString())
        {
            if (targetRole == Domain.Enums.Role.Admin)
            {
                logger.LogWarning(
                    "User {UserId} with role {Role} attempted to create Admin account",
                    userIdentityService.UserId,
                    currentUserRole);
                
                return Error.Forbidden(ErrorCodes.Account.InsufficientPermissions);
            }
            
            return null; // Permission granted
        }

        // Permission denied for other roles
        logger.LogWarning(
            "User {UserId} with role {Role} attempted to create account with role {TargetRole}",
            userIdentityService.UserId,
            currentUserRole,
            targetRole);

        return Error.Forbidden(ErrorCodes.Account.InsufficientPermissions);
    }

    /* EJEMPLO: Validación por Scopes (comentado, no se usa actualmente)
    /// <summary>
    /// Ejemplo de validación basada en scopes en lugar de roles.
    /// Usa constantes type-safe de Scopes para evitar errores de strings.
    /// </summary>
    private ErrorOr<Unit>? ValidateUserPermissionsByScope(Domain.Enums.Role targetRole)
    {
        if (!userIdentityService.IsAuthenticated)
        {
            return null; // Allow unauthenticated requests (e.g., initial setup)
        }

        // Definir scopes requeridos por rol usando constantes
        var requiredScope = targetRole switch
        {
            Domain.Enums.Role.Admin => Domain.Constants.Scopes.Accounts.CreateAdmin,
            Domain.Enums.Role.Developer => Domain.Constants.Scopes.Accounts.CreateDeveloper,
            Domain.Enums.Role.Manager => Domain.Constants.Scopes.Accounts.CreateManager,
            Domain.Enums.Role.User => Domain.Constants.Scopes.Accounts.CreateUser,
            _ => Domain.Constants.Scopes.Accounts.Create
        };

        // Verificar si el usuario tiene el scope requerido
        if (userIdentityService.HasScope(requiredScope))
        {
            logger.LogInformation(
                "User {UserId} has scope {Scope} to create {Role}",
                userIdentityService.UserId,
                requiredScope,
                targetRole);
            
            return null; // Permission granted
        }

        // También permitir si tiene el scope general de administración
        if (userIdentityService.HasScope(Domain.Constants.Scopes.Accounts.Manage))
        {
            return null; // Permission granted
        }

        // Permission denied
        var userScopes = userIdentityService.GetScopes().ToList();
        logger.LogWarning(
            "User {UserId} with scopes [{Scopes}] attempted to create account with role {TargetRole}. Required scope: {RequiredScope}",
            userIdentityService.UserId,
            string.Join(", ", userScopes),
            targetRole,
            requiredScope);

        return Error.Forbidden(ErrorCodes.Account.InsufficientPermissions);
    }
    */

    /* EJEMPLO ALTERNATIVO: Usando métodos de extensión (más conveniente)
    /// <summary>
    /// Ejemplo usando métodos de extensión para mayor conveniencia.
    /// </summary>
    private ErrorOr<Unit>? ValidateUserPermissionsByScopeWithExtensions(Domain.Enums.Role targetRole)
    {
        if (!userIdentityService.IsAuthenticated)
        {
            return null;
        }

        // Usar métodos de extensión (más legible)
        var hasPermission = targetRole switch
        {
            Domain.Enums.Role.Admin => userIdentityService.CanCreateAdmin(),
            Domain.Enums.Role.Developer => userIdentityService.CanCreateDeveloper(),
            Domain.Enums.Role.Manager => userIdentityService.CanCreateManager(),
            Domain.Enums.Role.User => userIdentityService.CanCreateUser(),
            _ => userIdentityService.CanCreateAccounts()
        };

        if (!hasPermission)
        {
            logger.LogWarning(
                "User {UserId} does not have permission to create {Role}",
                userIdentityService.UserId,
                targetRole);
            
            return Error.Forbidden(ErrorCodes.Account.InsufficientPermissions);
        }

        return null; // Permission granted
    }
    */
}
