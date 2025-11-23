using Acme.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acme.Infrastructure.Persistence.EF;

/// <summary>
/// Service for applying Entity Framework Core migrations.
/// </summary>
public class DatabaseMigrator(ApplicationDbContext applicationDbContext, ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    private readonly ApplicationDbContext applicationDbContext = applicationDbContext;
    private readonly ILogger<DatabaseMigrator> logger = logger;
    
    // Extended timeout for migrations (some migrations can take several minutes)
    private static readonly TimeSpan MigrationTimeout = TimeSpan.FromMinutes(5);

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database migrations...");

        try
        {
            // Set extended command timeout for migrations
            var previousTimeout = applicationDbContext.Database.GetCommandTimeout();
            applicationDbContext.Database.SetCommandTimeout(MigrationTimeout);

            // MigrateAsync will apply migrations
            // Note: For PostgreSQL, we rely on the container to create the database (via POSTGRES_DB env var)
            // or the connection string pointing to an existing DB.
            var pendingMigrations = await applicationDbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", 
                    pendingMigrations.Count(), 
                    string.Join(", ", pendingMigrations));
                    
                await applicationDbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("No pending migrations. Database is up to date.");
            }
            
            // Restore previous timeout
            applicationDbContext.Database.SetCommandTimeout(previousTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying database migrations.");
            throw;
        }
    }
}
