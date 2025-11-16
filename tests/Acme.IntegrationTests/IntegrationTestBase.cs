using Acme.Domain.Entities;
using Acme.Infrastructure.Persistence.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Acme.IntegrationTests;

/// <summary>
/// Base class for integration test fixtures.
/// </summary>
public abstract class IntegrationTestBase(string testName) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"{testName}Database_{Guid.NewGuid():N}";

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await DropDatabaseAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Default settings
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = GetLocalDbConnectionString(),
                ["TestEnvironment"] = "true"
            };

            // Allow derived classes to override or add settings
            ConfigureSettings(settings);

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            db.Database.Migrate();
            SeedTestData(userManager, roleManager).GetAwaiter().GetResult();
        });
    }

    /// <summary>
    /// Override this method to customize configuration settings for tests.
    /// The dictionary already contains default settings that can be modified or extended.
    /// </summary>
    /// <param name="settings">Dictionary of configuration settings</param>
    protected virtual void ConfigureSettings(Dictionary<string, string?> settings)
    {
        // Default implementation does nothing - derived classes can override
    }

    protected abstract Task SeedTestData(UserManager<Account> userManager, RoleManager<IdentityRole> roleManager);

    private string GetLocalDbConnectionString()
    {
        return $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    }

    private async Task DropDatabaseAsync()
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(GetLocalDbConnectionString());
            
            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            
            // Log database cleanup attempt
            Console.WriteLine($"[IntegrationTest] Attempting to drop database: {_databaseName}");
            
            var deleted = await context.Database.EnsureDeletedAsync();
            
            if (deleted)
            {
                Console.WriteLine($"[IntegrationTest] Successfully dropped database: {_databaseName}");
            }
            else
            {
                Console.WriteLine($"[IntegrationTest] Database did not exist or was already deleted: {_databaseName}");
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - cleanup failures shouldn't fail tests
            Console.WriteLine($"[IntegrationTest] Warning: Failed to drop database {_databaseName}. Error: {ex.Message}");
            
            // Optional: Try alternative cleanup method
            await TryForceDropDatabaseAsync();
        }
    }

    private async Task TryForceDropDatabaseAsync()
    {
        try
        {
            Console.WriteLine($"[IntegrationTest] Attempting force drop of database: {_databaseName}");
            
            var masterConnectionString = "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(masterConnectionString);
            
            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            
            // Use parameterized query to avoid SQL injection warning
            await context.Database.ExecuteSqlAsync($@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = {_databaseName})
                BEGIN
                    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{_databaseName}];
                END");
            
            Console.WriteLine($"[IntegrationTest] Force drop successful for database: {_databaseName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IntegrationTest] Force drop also failed for {_databaseName}. Error: {ex.Message}");
        }
    }
}
