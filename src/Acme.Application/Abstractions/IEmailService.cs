namespace Acme.Application.Abstractions;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends welcome email with generated password to new user.
    /// </summary>
    Task SendWelcomeWithPasswordAsync(string email, string temporaryPassword, CancellationToken cancellationToken = default);
}
