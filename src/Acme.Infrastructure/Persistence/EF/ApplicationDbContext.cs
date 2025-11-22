namespace Acme.Infrastructure.Persistence.EF;

using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<Account>(options), IApplicationDbContext
{
    public const string Schema = "dbo";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    // Core authentication tables
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<OpenIddictEntityFrameworkCoreApplication> OpenIddictApplications => Set<OpenIddictEntityFrameworkCoreApplication>();
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization> OpenIddictAuthorizations => Set<OpenIddictEntityFrameworkCoreAuthorization>();
    public DbSet<OpenIddictEntityFrameworkCoreScope> OpenIddictScopes => Set<OpenIddictEntityFrameworkCoreScope>();
    public DbSet<OpenIddictEntityFrameworkCoreToken> OpenIddictTokens => Set<OpenIddictEntityFrameworkCoreToken>();

    public DbSet<Account> Accounts => Set<Account>();
    
    // Example entity - TodoItem demonstrates CRUD patterns
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Example: Global query filter for soft delete
        // builder.Entity<TodoItem>().HasQueryFilter(t => t.DeletedAt == null);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Suppress pending model changes warning for integration tests
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    /// <summary>
    /// Overrides SaveChangesAsync to automatically set audit timestamps.
    /// Uncomment and customize for your entities.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Example: Automatic timestamp updates
        // var entries = ChangeTracker.Entries()
        //     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        //
        // foreach (var entry in entries)
        // {
        //     if (entry.Entity is TodoItem todo)
        //     {
        //         if (entry.State == EntityState.Added)
        //         {
        //             todo.CreatedAt = DateTime.UtcNow;
        //         }
        //     }
        // }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
