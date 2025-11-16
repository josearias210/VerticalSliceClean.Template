using Acme.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Acme.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically cleans up expired refresh tokens from the database.
/// Runs daily at 3 AM to remove tokens that have passed their expiration date.
/// </summary>
public sealed class TokenCleanupService(
    IServiceProvider serviceProvider,
    ILogger<TokenCleanupService> logger) : BackgroundService
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<TokenCleanupService> logger = logger;
    private readonly TimeSpan cleanupInterval = TimeSpan.FromHours(24);
    private readonly TimeSpan targetTime = new(3, 0, 0); // 3 AM

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Token Cleanup Service started.");

        // Wait until the target time on first run
        await WaitUntilTargetTime(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while cleaning up expired tokens.");
            }

            // Wait 24 hours before next cleanup
            await Task.Delay(cleanupInterval, stoppingToken);
        }

        logger.LogInformation("Token Cleanup Service stopped.");
    }

    /// <summary>
    /// Waits until the target time (3 AM) before running the first cleanup.
    /// </summary>
    private async Task WaitUntilTargetTime(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var targetDateTime = now.Date.Add(targetTime);

        // If we've already passed 3 AM today, schedule for 3 AM tomorrow
        if (now > targetDateTime)
        {
            targetDateTime = targetDateTime.AddDays(1);
        }

        var delay = targetDateTime - now;
        
        logger.LogInformation(
            "Token cleanup scheduled to run at {TargetTime}. Next run in {TotalHours:F1} hours.",
            targetDateTime,
            delay.TotalHours
        );

        await Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Deletes all refresh tokens that have expired.
    /// </summary>
    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting cleanup of expired refresh tokens...");

        // Create a new scope to get DbContext (IHostedService is singleton)
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Delete expired tokens (both revoked and expired)
        var deletedCount = await dbContext.RefreshTokens
            .Where(t => t.ExpiresAt < now || t.RevokedAt != null)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation(
            "Token cleanup completed. Deleted {DeletedCount} expired/revoked tokens.",
            deletedCount
        );

        // Optional: Log statistics about remaining tokens
        var activeTokensCount = await dbContext.RefreshTokens
            .CountAsync(t => t.ExpiresAt > now && t.RevokedAt == null, cancellationToken);

        logger.LogInformation(
            "Current active tokens: {ActiveCount}",
            activeTokensCount
        );
    }
}
