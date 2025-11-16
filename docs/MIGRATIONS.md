# Database Migrations Guide

This template includes a base migration with authentication tables. Follow this guide to manage migrations in your project.

## 📋 Base Migration Included

The template comes with `InitialCreate` migration that includes:
- ✅ **ASP.NET Identity tables** (Users, Roles, etc.)
- ✅ **Account** entity (extends IdentityUser)
- ✅ **RefreshToken** entity (for JWT token rotation)
- ✅ **Admin user seeding** with default roles

## 🚀 Getting Started with New Project

### 1. First Run (Automatic Setup)
```powershell
cd src/[YourProject].AppHost
dotnet run
```

The application will automatically:
- Create the database if it doesn't exist
- Apply all pending migrations
- Seed roles (Admin, Manager, ProductManager, User)
- Create the admin user

### 2. Verify Database
```sql
-- Check tables created
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo';

-- Expected tables:
-- - AspNetUsers, AspNetRoles, AspNetUserRoles, etc. (Identity)
-- - Accounts (Account entity - aliased to AspNetUsers)
-- - RefreshTokens
```

## 📝 Adding Your Domain Entities

### Step 1: Create Entity
```csharp
// src/[YourProject].Domain/Entities/TodoItem.cs
public sealed class TodoItem
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Step 2: Add DbSet to ApplicationDbContext
```csharp
// src/[YourProject].Infrastructure/Persistence/EF/ApplicationDbContext.cs
public DbSet<TodoItem> TodoItems => Set<TodoItem>();
```

### Step 3: Create Configuration (Optional but Recommended)
```csharp
// src/[YourProject].Infrastructure/Persistence/EF/Configurations/TodoItemConfiguration.cs
public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems", ApplicationDbContext.Schema);
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        // ... more configuration
    }
}
```

### Step 4: Create Migration
```powershell
cd src/[YourProject].Infrastructure

# Create migration
dotnet ef migrations add AddTodoItems --startup-project ../[YourProject].AppHost

# Review migration file
# src/[YourProject].Infrastructure/Persistence/EF/Migrations/[timestamp]_AddTodoItems.cs
```

### Step 5: Apply Migration
```powershell
# Option A: Run application (auto-applies migrations)
cd src/[YourProject].AppHost
dotnet run

# Option B: Manual apply
cd src/[YourProject].Infrastructure
dotnet ef database update --startup-project ../[YourProject].AppHost
```

## 🔄 Common Migration Commands

### Create Migration
```powershell
cd src/[YourProject].Infrastructure
dotnet ef migrations add MigrationName --startup-project ../[YourProject].AppHost
```

### Apply Migrations
```powershell
# Apply all pending
dotnet ef database update --startup-project ../[YourProject].AppHost

# Apply specific migration
dotnet ef database update TargetMigrationName --startup-project ../[YourProject].AppHost
```

### Rollback Migration
```powershell
# Rollback to previous
dotnet ef database update PreviousMigrationName --startup-project ../[YourProject].AppHost

# Rollback all (drop database)
dotnet ef database update 0 --startup-project ../[YourProject].AppHost
```

### Remove Last Migration (Not Applied)
```powershell
dotnet ef migrations remove --startup-project ../[YourProject].AppHost
```

### List Migrations
```powershell
dotnet ef migrations list --startup-project ../[YourProject].AppHost
```

### Generate SQL Script
```powershell
# Generate script for all migrations
dotnet ef migrations script --startup-project ../[YourProject].AppHost -o migration.sql

# Generate script for specific range
dotnet ef migrations script FromMigration ToMigration --startup-project ../[YourProject].AppHost
```

## 🏭 Production Deployment

### Option 1: Application Auto-Migration (Development/Staging)
The application automatically runs migrations on startup via `DatabaseMigrator`:
```csharp
// Runs automatically in Program.cs
await app.ApplyMigrationsAsync();
```

**Pros:**
- ✅ Simple, no manual steps
- ✅ Good for dev/staging

**Cons:**
- ❌ Not recommended for production (requires elevated DB permissions)
- ❌ Downtime during migration
- ❌ No rollback strategy

### Option 2: SQL Scripts (Production Recommended)
Generate and review SQL scripts before deployment:

```powershell
# 1. Generate script
cd src/[YourProject].Infrastructure
dotnet ef migrations script --startup-project ../[YourProject].AppHost -o deploy.sql --idempotent

# 2. Review script manually
# 3. Execute via SQL Server Management Studio or Azure DevOps

# 4. Disable auto-migration in production
# appsettings.Production.json
{
  "DatabaseSettings": {
    "AutoMigrate": false
  }
}
```

**Pros:**
- ✅ Full control and review
- ✅ Can execute during maintenance window
- ✅ Supports complex rollback scenarios
- ✅ Meets audit requirements

**Cons:**
- ❌ Manual process
- ❌ Requires DBA coordination

### Option 3: CI/CD Pipeline
```yaml
# Example: Azure DevOps pipeline
- task: DotNetCoreCLI@2
  displayName: 'Generate Migration Script'
  inputs:
    command: 'custom'
    custom: 'ef'
    arguments: 'migrations script --startup-project src/[YourProject].AppHost --output $(Build.ArtifactStagingDirectory)/migration.sql --idempotent'
    workingDirectory: 'src/[YourProject].Infrastructure'

- task: SqlAzureDacpacDeployment@1
  displayName: 'Apply Migration'
  inputs:
    azureSubscription: '$(AzureSubscription)'
    ScriptType: 'InlineSqlScript'
    InlineSql: '$(Build.ArtifactStagingDirectory)/migration.sql'
```

## 🧪 Development Workflow

### Iterating on Migrations (Before Committing)
```powershell
# 1. Create migration
dotnet ef migrations add FeatureName --startup-project ../[YourProject].AppHost

# 2. Test by applying
dotnet ef database update --startup-project ../[YourProject].AppHost

# 3. Found issue? Remove and recreate
dotnet ef migrations remove --startup-project ../[YourProject].AppHost
# Fix entity configuration
dotnet ef migrations add FeatureName --startup-project ../[YourProject].AppHost
```

### Reset Database (Fresh Start)
```powershell
# Option A: Drop and recreate
dotnet ef database drop --startup-project ../[YourProject].AppHost --force
dotnet ef database update --startup-project ../[YourProject].AppHost

# Option B: Rollback all migrations
dotnet ef database update 0 --startup-project ../[YourProject].AppHost
dotnet ef database update --startup-project ../[YourProject].AppHost
```

## ⚠️ Best Practices

### DO ✅
- **Review migration files** before applying
- **Use meaningful names**: `AddTodoItems` not `Migration1`
- **Keep migrations small**: One feature per migration
- **Test rollback** before deploying to production
- **Generate SQL scripts** for production
- **Version control migrations**: Commit migration files to Git
- **Use idempotent scripts**: `--idempotent` flag

### DON'T ❌
- **Don't modify applied migrations**: Create new migration instead
- **Don't delete migration files**: Breaks migration history
- **Don't auto-migrate in production**: Use SQL scripts
- **Don't mix schema and data changes**: Separate migrations
- **Don't ignore migration warnings**: They indicate potential issues

## 🔧 Troubleshooting

### "A network-related or instance-specific error occurred"
```powershell
# Check connection string in User Secrets
cd src/[YourProject].Infrastructure
dotnet user-secrets list

# Verify SQL Server is running
docker ps  # If using Docker
```

### "The migration has already been applied to the database"
```powershell
# Check applied migrations
dotnet ef migrations list --startup-project ../[YourProject].AppHost

# If out of sync, manually update __EFMigrationsHistory table
```

### "Cannot open database requested by the login"
The application will create the database automatically on first run via `DatabaseMigrator.cs`.

### "A transaction is not allowed when using SqlExecutionStrategy"
The template includes `ExecutionStrategy` handling in `DatabaseMigrator.cs` with distributed locking.

## 📚 Further Reading

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core Best Practices](https://learn.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext)
- [Production Deployment Strategies](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

---

**Need Help?** Check `docs/adr/` for architectural decisions or open an issue in the repository.
