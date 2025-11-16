namespace Acme.Infrastructure.Persistence.EF;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances during migrations.
/// Automatically loads User Secrets in development environment.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new ConfigurationBuilder();
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Determine environment
        var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") 
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) 
            || devEnvironmentVariable.Equals("development", StringComparison.OrdinalIgnoreCase);

        // Load configuration sources
        if (isDevelopment)
        {
            builder.AddUserSecrets<ApplicationDbContextFactory>();
        }

        builder.AddEnvironmentVariables();

        var configuration = builder.Build();
        
        // Validate connection string exists
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Ensure it's configured in User Secrets (dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\") " +
                "or environment variables.");
        }

        // Configure SQL Server with migrations history table
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.MigrationsHistoryTable(
                    ApplicationDbContext.MigrationsHistoryTable,
                    ApplicationDbContext.Schema);
            });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
