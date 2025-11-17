using Acme.Application.Abstractions;
using Acme.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Acme.Host.Extensions;

/// <summary>
/// Extension methods for database initialization during application startup.
/// </summary>
public static class DatabaseStartupExtensions
{
    /// <summary>
    /// Initializes database with migrations and seed data based on configuration.
    /// Throws exception on failure to prevent application startup with invalid database state.
    /// </summary>
    public static async Task InitializeDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

        try
        {
            var databaseSettings = app.Services.GetRequiredService<IOptions<DatabaseSettings>>().Value;

            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;

            if (databaseSettings.ApplyMigrationsOnStartup)
            {
                logger.LogInformation("Applying database migrations...");
                var migrator = sp.GetRequiredService<IDatabaseMigrator>();
                await migrator.ApplyMigrationsAsync(cancellationToken);
            }
            else
            {
                logger.LogInformation("Automatic migrations disabled. Skipping migration check.");
            }

            if (databaseSettings.SeedRolesOnStartup)
            {
                logger.LogInformation("Seeding roles...");
                await sp.GetRequiredService<IDatabaseSeeder>().SeedRolesAsync(cancellationToken);
            }

            if (databaseSettings.SeedAdminOnStartup)
            {
                logger.LogInformation("Seeding admin user...");
                await sp.GetRequiredService<IDatabaseSeeder>().SeedAdminUserAsync(cancellationToken);
            }

            logger.LogInformation("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed. Application cannot start with invalid database state.");
            throw new InvalidOperationException("Database initialization failed during application startup. See inner exception for details.", ex);
        }
    }
}
