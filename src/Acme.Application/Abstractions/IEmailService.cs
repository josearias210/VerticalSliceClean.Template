// -----------------------------------------------------------------------
// <copyright file="IEmailService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Abstractions;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends email confirmation link to user.
    /// </summary>
    Task SendEmailConfirmationAsync(string email, string confirmationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends password reset link to user.
    /// </summary>
    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends welcome email with generated password to new user.
    /// </summary>
    Task SendWelcomeWithPasswordAsync(string email, string temporaryPassword, CancellationToken cancellationToken = default);
}
