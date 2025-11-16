using Acme.Infrastructure.Persistence.EF;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Acme.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies database connectivity.
/// Does not log on success to avoid log noise (health checks run frequently).
/// </summary>
public sealed class DbContextConnectivityHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    private readonly ApplicationDbContext dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            
            if (canConnect)
            {
                // Get additional database info
                var connectionString = dbContext.Database.GetConnectionString();
                var builder = new SqlConnectionStringBuilder(connectionString);
                var serverName = builder.DataSource;
                var databaseName = builder.InitialCatalog;

                return HealthCheckResult.Healthy(
                    "Database is reachable",
                    data: new Dictionary<string, object>
                    {
                        ["Server"] = serverName,
                        ["Database"] = databaseName
                    });
            }

            return HealthCheckResult.Unhealthy("Database is unreachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection check failed",
                exception: ex);
        }
    }
}
