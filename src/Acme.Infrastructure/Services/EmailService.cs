namespace Acme.Infrastructure.Services;

using Acme.Application.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Email service implementation that logs emails instead of sending them.
/// Replace with real SMTP/SendGrid/AWS SES implementation in production.
/// </summary>
public class EmailService(ILogger<EmailService> logger) : IEmailService
{
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
