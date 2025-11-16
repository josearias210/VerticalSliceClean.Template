// -----------------------------------------------------------------------
// <copyright file="ISecurityAuditService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Abstractions;

public interface ISecurityAuditService
{
    Task LogSecurityEventAsync(
        string userId,
        string eventType,
        string? eventDetails = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}
