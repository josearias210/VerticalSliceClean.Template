// -----------------------------------------------------------------------
// <copyright file="ProductService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Features.Account.RegisterAccount;

using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

public class RegisterAccountCommandHandler(
    UserManager<Account> userManager,
    IEmailService emailService,
    ILogger<RegisterAccountCommandHandler> logger) : IRequestHandler<RegisterAccountCommand, ErrorOr<Unit>>
{
    private readonly UserManager<Account> userManager = userManager;
    private readonly IEmailService emailService = emailService;
    private readonly ILogger<RegisterAccountCommandHandler> logger = logger;

    public async Task<ErrorOr<Unit>> Handle(RegisterAccountCommand registerAccountCommand, CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "RegisterAccount",
            ["Email"] = registerAccountCommand.Email
        });

        logger.LogInformation("Registering new account for {Email}", registerAccountCommand.Email);

        // Generate random secure password (16 characters with special chars)
        var temporaryPassword = GenerateSecurePassword(16);
        
        var account = new Account 
        { 
            UserName = registerAccountCommand.Email, 
            Email = registerAccountCommand.Email,
            EmailConfirmed = false // Will be confirmed via email
        };
        
        var createResult = await userManager.CreateAsync(account, temporaryPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Account creation failed for {Email}: {Errors}",
                registerAccountCommand.Email,
                string.Join(", ", createResult.Errors.Select(e => e.Code)));
            return Error.Failure("Account.CreateFailed", "Failed to create account");
        }

        var roleResult = await userManager.AddToRoleAsync(account, registerAccountCommand.Password); // Use Password field as Role
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to add role {Role} to account {Email}: {Errors}",
                registerAccountCommand.Password,
                registerAccountCommand.Email,
                string.Join(", ", roleResult.Errors.Select(e => e.Code)));
            return Error.Failure("Account.RoleAssignFailed", "Failed to assign role");
        }

        // Send welcome email with temporary password
        try
        {
            await emailService.SendWelcomeWithPasswordAsync(
                registerAccountCommand.Email, 
                temporaryPassword, 
                cancellationToken);
            
            logger.LogInformation(
                "Account registered successfully for {Email} with role {Role}. Welcome email sent.",
                registerAccountCommand.Email,
                registerAccountCommand.Password);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", registerAccountCommand.Email);
            // Don't fail registration if email fails
        }

        return Unit.Value;
    }

    /// <summary>
    /// Generates a cryptographically secure random password.
    /// </summary>
    private static string GenerateSecurePassword(int length)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=";
        var password = new char[length];
        
        using var rng = RandomNumberGenerator.Create();
        var data = new byte[length];
        rng.GetBytes(data);
        
        for (int i = 0; i < length; i++)
        {
            password[i] = validChars[data[i] % validChars.Length];
        }
        
        return new string(password);
    }
}
