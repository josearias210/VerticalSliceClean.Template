// -----------------------------------------------------------------------
// <copyright file="EmailService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Infrastructure.Services;

using Acme.Application.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Email service implementation that logs emails instead of sending them.
/// Replace with real SMTP/SendGrid/AWS SES implementation in production.
/// </summary>
public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    private readonly ILogger<EmailService> logger = logger;

    public Task SendEmailConfirmationAsync(string email, string confirmationToken, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            """
            ===============================================
            📧 EMAIL CONFIRMATION
            ===============================================
            To: {Email}
            Subject: Confirm your email address
            
            Please confirm your email by clicking the link below:
            
            Confirmation Token: {Token}
            
            (In production, this would be a clickable URL)
            ===============================================
            """,
            email,
            confirmationToken);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            """
            ===============================================
            🔐 PASSWORD RESET
            ===============================================
            To: {Email}
            Subject: Reset your password
            
            You requested a password reset. Use the token below:
            
            Reset Token: {Token}
            
            (In production, this would be a clickable URL)
            ===============================================
            """,
            email,
            resetToken);

        return Task.CompletedTask;
    }

    public Task SendWelcomeWithPasswordAsync(string email, string temporaryPassword, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            """
            ===============================================
            👋 WELCOME - YOUR ACCOUNT IS READY
            ===============================================
            To: {Email}
            Subject: Welcome! Your account has been created
            
            Your account has been successfully created.
            
            Email: {Email}
            Temporary Password: {TemporaryPassword}
            
            ⚠️ IMPORTANT: Please change your password after first login for security reasons.
            
            Login at: /api/v1/accounts/login
            ===============================================
            """,
            email,
            email,
            temporaryPassword);

        return Task.CompletedTask;
    }
}
