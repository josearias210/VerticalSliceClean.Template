namespace Acme.Application.Features.Account.GetProfile;

using ErrorOr;
using Acme.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetProfileQueryHandler(
    IUserIdentityService userIdentityService,
    IApplicationDbContext dbContext,
    ILogger<GetProfileQueryHandler> logger) : IRequestHandler<GetProfileQuery, ErrorOr<GetProfileQueryResponse>>
{
    private readonly IUserIdentityService userIdentityService = userIdentityService;
    private readonly IApplicationDbContext dbContext = dbContext;
    private readonly ILogger<GetProfileQueryHandler> logger = logger;

    public async Task<ErrorOr<GetProfileQueryResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = userIdentityService.GetUserId();
        var email = userIdentityService.GetEmail();

        if (userId == null || email == null)
        {
            logger.LogWarning("Unauthorized access attempt to get profile");
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials");
        }

        var account = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == userId, cancellationToken);
        if (account == null)
        {
            logger.LogWarning("Account not found for user {UserId}", userId);
            return Error.NotFound("Account.NotFound", "Account not found");
        }

        logger.LogInformation("Profile retrieved successfully for user {UserId}", userId);
        return new GetProfileQueryResponse
        {
            UserId = account.Id.ToString(),
            Email = account.Email ?? string.Empty,
        };
    }
}
