// -----------------------------------------------------------------------
// <copyright file="ProductService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Features.Account.Login;

using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class LoginCommandHandler(
    ITokenService tokenService,
    UserManager<Account> userManager,
    ICookieTokenService cookieTokenService,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, ErrorOr<LoginCommandResponse>>
{
    private readonly UserManager<Account> userManager = userManager;
    private readonly ITokenService tokenService = tokenService;
    private readonly ICookieTokenService cookieTokenService = cookieTokenService;
    private readonly ILogger<LoginCommandHandler> logger = logger;

    public async Task<ErrorOr<LoginCommandResponse>> Handle(LoginCommand loginCommand, CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "Login",
            ["Email"] = loginCommand.Email
        });

        logger.LogInformation("Login attempt for {Email}", loginCommand.Email);

        var account = await userManager.FindByEmailAsync(loginCommand.Email);
        if (account == null)
        {
            logger.LogWarning("Login failed: account not found for {Email}", loginCommand.Email);
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials");
        }

        // Check if account is locked out
        if (await userManager.IsLockedOutAsync(account))
        {
            var lockoutEnd = await userManager.GetLockoutEndDateAsync(account);
            logger.LogWarning("Login attempt for locked account {Email}. Lockout until {LockoutEnd}", 
                loginCommand.Email, lockoutEnd);
            return Error.Forbidden(
                "Auth.AccountLocked", 
                $"Account is locked due to multiple failed login attempts. Try again after {lockoutEnd:yyyy-MM-dd HH:mm:ss} UTC");
        }

        var valid = await userManager.CheckPasswordAsync(account, loginCommand.Password);
        if (!valid)
        {
            // Increment failed access count and potentially lock account
            await userManager.AccessFailedAsync(account);
            
            var failedCount = await userManager.GetAccessFailedCountAsync(account);
            var maxAttempts = userManager.Options.Lockout.MaxFailedAccessAttempts;
            
            logger.LogWarning(
                "Login failed for {Email}. Failed attempts: {FailedCount}/{MaxAttempts}", 
                loginCommand.Email, failedCount, maxAttempts);
            
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials");
        }

        // Reset failed access count on successful login
        await userManager.ResetAccessFailedCountAsync(account);

        var tokens = await tokenService.CreateTokensAsync(account);
        if (tokens.IsError)
        {
            logger.LogError("Token creation failed for {Email}: {Errors}",
                loginCommand.Email,
                string.Join(", ", tokens.Errors.Select(e => e.Code)));
            return tokens.Errors;
        }

        // Set tokens as httpOnly cookies
        cookieTokenService.SetTokenCookies(tokens.Value.accessToken, tokens.Value.refreshToken);

        var roles = await userManager.GetRolesAsync(account);
        
        logger.LogInformation("Login successful for {Email}", loginCommand.Email);
        return new LoginCommandResponse 
        { 
            UserId = account.Id,
            Email = account.Email ?? string.Empty,
            FullName = account.FullName,
            Roles = roles,
            EmailConfirmed = account.EmailConfirmed
        };
    }
}
