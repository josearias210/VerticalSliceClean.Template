using Acme.Application.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Acme.Infrastructure.Persistence.EF;

/// <summary>
/// Service for applying Entity Framework Core migrations.
/// Uses distributed lock to prevent concurrent migration execution.
/// </summary>
public class DatabaseMigrator(ApplicationDbContext applicationDbContext, ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    private readonly ApplicationDbContext applicationDbContext = applicationDbContext;
    private readonly ILogger<DatabaseMigrator> logger = logger;
    
    // Extended timeout for migrations (some migrations can take several minutes)
    private static readonly TimeSpan MigrationTimeout = TimeSpan.FromMinutes(5);
    private const string LockName = "Acme.DbMigration";

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database migrations...");

        try
        {
            // Ensure database exists (MigrateAsync does NOT create the database in SQL Server)
            await EnsureDatabaseExistsAsync(cancellationToken);
            
            // Set extended command timeout for migrations
            var previousTimeout = applicationDbContext.Database.GetCommandTimeout();
            applicationDbContext.Database.SetCommandTimeout(MigrationTimeout);

            // MigrateAsync will apply migrations
            var pendingMigrations = await applicationDbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", 
                    pendingMigrations.Count(), 
                    string.Join(", ", pendingMigrations));
                    
                // Try to acquire distributed lock
                var lockAcquired = await TryAcquireMigrationLockAsync(cancellationToken);
                
                if (!lockAcquired)
                {
                    logger.LogInformation("Another instance is currently applying migrations. Skipping.");
                    return;
                }

                try
                {
                    await applicationDbContext.Database.MigrateAsync(cancellationToken);
                    logger.LogInformation("Database migrations applied successfully.");
                }
                finally
                {
                    await ReleaseMigrationLockAsync(cancellationToken);
                }
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

    /// <summary>
    /// Ensures the database exists. Creates it if it doesn't.
    /// </summary>
    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var connection = applicationDbContext.Database.GetDbConnection();
        var databaseName = connection.Database;
        
        // Build connection string to master database
        var builder = new SqlConnectionStringBuilder(connection.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var masterConnection = new SqlConnection(builder.ConnectionString);
        await masterConnection.OpenAsync(cancellationToken);
        
        // Check if database exists
        await using var checkCommand = masterConnection.CreateCommand();
        checkCommand.CommandText = $"SELECT database_id FROM sys.databases WHERE Name = @databaseName";
        checkCommand.Parameters.Add(new SqlParameter("@databaseName", databaseName));
        
        var exists = await checkCommand.ExecuteScalarAsync(cancellationToken);
        
        if (exists == null)
        {
            logger.LogInformation("Database '{DatabaseName}' does not exist. Creating...", databaseName);
            
            // Create database
            await using var createCommand = masterConnection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE [{databaseName}]";
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
            
            logger.LogInformation("Database '{DatabaseName}' created successfully.", databaseName);
        }
        else
        {
            logger.LogInformation("Database '{DatabaseName}' already exists.", databaseName);
        }
    }

    /// <summary>
    /// Tries to acquire a distributed lock for migrations using SQL Server sp_getapplock.
    /// Returns true if lock was acquired, false if another instance holds the lock.
    /// </summary>
    private async Task<bool> TryAcquireMigrationLockAsync(CancellationToken cancellationToken)
    {
        var connection = applicationDbContext.Database.GetDbConnection();
        
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "sp_getapplock";
        command.CommandType = System.Data.CommandType.StoredProcedure;
        
        command.Parameters.Add(new SqlParameter("@Resource", LockName));
        command.Parameters.Add(new SqlParameter("@LockMode", "Exclusive"));
        command.Parameters.Add(new SqlParameter("@LockOwner", "Session"));
        command.Parameters.Add(new SqlParameter("@LockTimeout", 0)); // 0 = immediate return
        
        var returnParam = new SqlParameter
        {
            Direction = System.Data.ParameterDirection.ReturnValue
        };
        command.Parameters.Add(returnParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        var result = (int)(returnParam.Value ?? -1);
        
        // Return codes: 0 or 1 = success, negative = failed to acquire
        return result >= 0;
    }

    /// <summary>
    /// Releases the distributed migration lock.
    /// </summary>
    private async Task ReleaseMigrationLockAsync(CancellationToken cancellationToken)
    {
        var connection = applicationDbContext.Database.GetDbConnection();
        
        if (connection.State != System.Data.ConnectionState.Open)
        {
            return; // Connection already closed, lock is released
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sp_releaseapplock @Resource, @LockOwner";
        
        command.Parameters.Add(new SqlParameter("@Resource", LockName));
        command.Parameters.Add(new SqlParameter("@LockOwner", "Session"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
