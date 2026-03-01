
namespace Acme.Infrastructure.HealthChecks;

using Acme.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check that verifies if there are pending database migrations.
/// Returns Unhealthy if migrations are pending, indicating the database is not up to date.
/// </summary>
public class MigrationHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    private readonly ApplicationDbContext dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingMigrations = await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
                var migrationsList = string.Join(", ", pendingMigrations);
                return HealthCheckResult.Unhealthy(
                    $"Database has {pendingMigrations.Count()} pending migration(s): {migrationsList}",
                    data: new Dictionary<string, object>
                    {
                        ["PendingMigrations"] = pendingMigrations.ToList(),
                        ["Count"] = pendingMigrations.Count()
                    });
            }

            return HealthCheckResult.Healthy("Database is up to date");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to check database migrations",
                exception: ex);
        }
    }
}
