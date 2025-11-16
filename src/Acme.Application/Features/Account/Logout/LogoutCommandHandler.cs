// -----------------------------------------------------------------------
// <copyright file="LogoutCommandHandler.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Features.Account.Logout;

using ErrorOr;
using Acme.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

public class LogoutCommandHandler(
    ICookieTokenService cookieTokenService,
    ILogger<LogoutCommandHandler> logger) : IRequestHandler<LogoutCommand, ErrorOr<bool>>
{
    private readonly ICookieTokenService cookieTokenService = cookieTokenService;
    private readonly ILogger<LogoutCommandHandler> logger = logger;

    public Task<ErrorOr<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("User logging out");
        
        // Clear auth cookies
        cookieTokenService.ClearTokenCookies();
        
        logger.LogInformation("Logout successful - cookies cleared");
        return Task.FromResult<ErrorOr<bool>>(true);
    }
}
