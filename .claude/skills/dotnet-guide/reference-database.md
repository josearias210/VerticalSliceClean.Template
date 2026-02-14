# Database Reference

## DbContext

**File:** `src/Acme.Infrastructure/Persistence/EF/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<Account>(options), IApplicationDbContext
{
    public const string? Schema = null;
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public DbSet<OpenIddictEntityFrameworkCoreApplication> OpenIddictApplications => Set<...>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        // Global query filters go here: builder.Entity<T>().HasQueryFilter(...)
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Hook point for automatic audit timestamps (CreatedAt, UpdatedAt)
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### PostgreSQL Registration

**File:** `src/Acme.Infrastructure/DependencyInjection.cs`

```csharp
services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.MigrationsHistoryTable(
            ApplicationDbContext.MigrationsHistoryTable,
            ApplicationDbContext.Schema);
    });

    if (environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();  // Show parameter values in logs
        options.EnableDetailedErrors();         // Detailed EF error messages
    }

    options.UseOpenIddict();
});
```

---

## Design-Time Factory

**File:** `src/Acme.Infrastructure/Persistence/EF/ApplicationDbContextFactory.cs`

Required for `dotnet ef migrations` to work without running the app:

```csharp
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Detect environment (NETCORE_ENVIRONMENT or ASPNETCORE_ENVIRONMENT)
        // 2. Load User Secrets in development
        // 3. Load environment variables
        // 4. Validate connection string exists (throws helpful error if missing)
        // 5. Configure Npgsql with migrations history table
    }
}
```

---

## Database Migrator

**File:** `src/Acme.Infrastructure/Persistence/EF/DatabaseMigrator.cs`

```csharp
public class DatabaseMigrator(ApplicationDbContext dbContext, ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    private static readonly TimeSpan MigrationTimeout = TimeSpan.FromMinutes(5);

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        // 1. Set extended command timeout (5 min for long migrations)
        // 2. Check for pending migrations
        // 3. Apply if any pending, log migration names
        // 4. Restore original timeout
    }
}
```

### Migration Commands

```bash
# From src/Acme.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../Acme.Host
dotnet ef database update --startup-project ../Acme.Host
```

---

## Database Seeder

**File:** `src/Acme.Infrastructure/Persistence/EF/DatabaseSeeder.cs`

### SeedRolesAsync

Reads all values from `Role` enum and creates missing Identity roles:

```csharp
var roles = Enum.GetValues<Role>();
foreach (var role in roles)
{
    if (!await roleManager.RoleExistsAsync(role.ToString()))
        await roleManager.CreateAsync(new IdentityRole(role.ToString()));
}
```

### SeedAdminUserAsync

Creates admin user with password strength validation:

1. Validates `AdminUserSettings` (email + password from config)
2. **Password strength check**: min 12 chars, uppercase + lowercase + digit + special
3. If user exists: ensures has Developer role
4. If new: creates user → assigns Developer role
5. **Rollback on failure**: deletes user if role assignment fails

```csharp
var addRoleResult = await userManager.AddToRoleAsync(user, adminRole);
if (!addRoleResult.Succeeded)
{
    await userManager.DeleteAsync(user);  // Rollback
    return;
}
```

### SeedOpenIddictClientAsync

Creates `"react-app"` as public OAuth2 client (SPA — no client secret):

```csharp
await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
{
    ClientId = "react-app",
    DisplayName = "React Application",
    ClientType = ClientTypes.Public,
    Permissions =
    {
        Permissions.Endpoints.Token,
        Permissions.Endpoints.Revocation,
        Permissions.GrantTypes.Password,
        Permissions.GrantTypes.RefreshToken,
        // Scopes: OpenId, Email, Profile, Roles, api, OfflineAccess
    }
});
```

---

## Database Startup

**File:** `src/Acme.Host/Extensions/DatabaseStartupExtensions.cs`

Called in `Program.cs` via `await app.InitializeDatabaseAsync()` before `ConfigurePipeline()`.

### Flow

```
1. Read DatabaseSettings flags
2. CreateScope() for scoped services
3. If ApplyMigrationsOnStartup → IDatabaseMigrator.ApplyMigrationsAsync()
4. If SeedRolesOnStartup → IDatabaseSeeder.SeedRolesAsync()
5. If Development + SeedAdminOnStartup → IDatabaseSeeder.SeedAdminUserAsync()
6. Always → IDatabaseSeeder.SeedOpenIddictClientAsync()
7. On failure → LogCritical + throw InvalidOperationException (prevents app startup)
```

### DatabaseSettings (`Database` section)

```json
{
    "Database": {
        "ApplyMigrationsOnStartup": false,
        "SeedRolesOnStartup": false,
        "SeedAdminOnStartup": false
    }
}
```

Development override (`appsettings.Development.json`): all set to `true`.

---

## Health Checks

**File:** `src/Acme.Infrastructure/Extensions/HealthChecksExtensions.cs`

### Registered Checks

| Name | Type | Tags | Timeout | Purpose |
|------|------|------|---------|---------|
| `database` | `DbContextConnectivityHealthCheck` | ready, db | 5s | Tests DB connection, reports server/database name |
| `migrations` | `MigrationHealthCheck` | ready, db, migrations | 10s | Lists pending migrations |
| `memory` | Inline | live | — | Reports allocated MB, GC stats. Degraded if > 1GB |
| `startup` | Inline | live | — | Always healthy (app is running) |

### Endpoints

| Path | Filter | Purpose |
|------|--------|---------|
| `/health/live` | tag = `"live"` | **Liveness probe** — K8s restarts if unhealthy |
| `/health/ready` | tag = `"ready"` | **Readiness probe** — K8s stops traffic if unhealthy |
| `/health` | All checks | Full health report with details |

All endpoints: 10s output cache, 200 for Healthy/Degraded, 503 for Unhealthy.

### Response Format

```json
{
    "status": "Healthy",
    "timestamp": "2024-01-15T10:30:00Z",
    "duration": "00:00:00.123",
    "checks": [
        {
            "name": "database",
            "status": "Healthy",
            "description": null,
            "duration": "00:00:00.045",
            "data": { "Server": "localhost", "Database": "AcmeDb" },
            "tags": ["ready", "db"]
        }
    ]
}
```

---

## Settings Registration Pattern

**File:** `src/Acme.Infrastructure/DependencyInjection.cs`

All settings follow the same pattern:

```csharp
services.AddOptions<DatabaseSettings>()
    .BindConfiguration("Database")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Registered settings:

| Settings Class | Config Section | Validated |
|---------------|---------------|-----------|
| `DatabaseSettings` | `Database` | Yes |
| `AdminUserSettings` | `Admin` | Yes |
| `JwtSettings` | `JwtSettings` | Yes |
| `SeqSettings` | `Logging:Seq` | Yes |
| `OpenTelemetrySettings` | `OpenTelemetry` | Yes |
| `CorsSettings` | `Cors` | Yes |
| `OpenIddictSettings` | `OpenIddict` | Yes (no annotations) |

All validated at startup — app fails fast if config is invalid.
