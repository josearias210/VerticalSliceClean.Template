# Using VerticalSliceClean API Template

This guide explains how to use this project as a template for creating new client APIs with Clean Architecture and Vertical Slice patterns.

## Installation

### Install Template Locally

From the root directory of this repository:

```powershell
# Install the template
dotnet new install .
```

To verify installation:

```powershell
# List installed templates
dotnet new list vsclean
```

## Creating a New Project

### Basic Usage

```powershell
# Create new project with default values
dotnet new vsclean -n Acme.CRM

# This creates:
# - Acme.CRM.Api
# - Acme.CRM.AppHost
# - Acme.CRM.Application
# - Acme.CRM.Domain
# - Acme.CRM.Infrastructure
```

### Full Customization

```powershell
dotnet new vsclean `
  -n Contoso.Sales `
  --ClientName Contoso `
  --ProjectSuffix Sales `
  --DatabaseName ContosoSalesDb `
  --AdminEmail admin@contoso.com `
  --AdminPassword "Secure@Pass123!" `
  --JwtIssuer https://api.contoso.com `
  --JwtAudience https://app.contoso.com `
  --CorsOrigin https://app.contoso.com
```

### Parameter Reference

| Parameter         | Description                              | Default                     |
|-------------------|------------------------------------------|-----------------------------|
| `-n, --name`      | Project name (required)                  | -                           |
| `--ClientName`    | Client/company name                      | `MyCompany`                 |
| `--ProjectSuffix` | Project suffix                           | `Lab`                       |
| `--DatabaseName`  | Database name                            | `MyCompanyDb`               |
| `--AdminEmail`    | Default admin email                      | `admin@mycompany.com`       |
| `--AdminPassword` | Default admin password                   | `Admin@123456`              |
| `--JwtIssuer`     | JWT issuer URL                           | `https://localhost:7001`    |
| `--JwtAudience`   | JWT audience URL                         | `https://localhost:7001`    |
| `--CorsOrigin`    | Allowed frontend origin                  | `https://localhost:5173`    |
| `--skipRestore`   | Skip automatic package restore           | `false`                     |

## After Project Creation

### 1. Configure User Secrets

Navigate to your new project's Infrastructure folder:

```powershell
cd src\[YourProjectName].Infrastructure

# Set database connection
dotnet user-secrets set "DatabaseSettings:ConnectionString" "Server=localhost,1433;Database=[DatabaseName];User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"

# Set JWT secret (generate a new one!)
dotnet user-secrets set "JwtSettings:SecretKey" "[Generate-Random-256bit-Key]"
```

### 2. Run Database Migrations

```powershell
cd src\[YourProjectName].Infrastructure

# Create initial migration (if needed)
dotnet ef migrations add InitialCreate --startup-project ../[YourProjectName].AppHost

# Apply migrations
dotnet ef database update --startup-project ../[YourProjectName].AppHost
```

### 3. Start Docker Dependencies

```powershell
# Start SQL Server, Jaeger, and Seq
docker-compose up -d
```

### 4. Run the Application

```powershell
cd src\[YourProjectName].AppHost
dotnet run
```

## Template Maintenance

### Uninstall Template

```powershell
dotnet new uninstall CleanVerticalSlice.Template
```

### Update Template

```powershell
# Uninstall old version
dotnet new uninstall CleanVerticalSlice.Template

# Install new version
cd [path-to-updated-template]
dotnet new install .
```

## Template Structure

The template automatically renames:

### Namespaces
- `VerticalSliceClean.*` → `[ClientName].[ProjectSuffix].*`
- Example: `Acme.CRM.Api`, `Acme.CRM.Application`, etc.

### Folders
- `VerticalSliceClean.Api` → `[ClientName].[ProjectSuffix].Api`
- `VerticalSliceClean.AppHost` → `[ClientName].[ProjectSuffix].AppHost`
- `VerticalSliceClean.Application` → `[ClientName].[ProjectSuffix].Application`
- `VerticalSliceClean.Domain` → `[ClientName].[ProjectSuffix].Domain`
- `VerticalSliceClean.Infrastructure` → `[ClientName].[ProjectSuffix].Infrastructure`

### Files
- Solution file renamed to `[ClientName].[ProjectSuffix].sln`
- All `.csproj` files renamed accordingly
- All namespace declarations updated

### Configuration Values
- Database name in connection strings
- Admin email/password defaults
- JWT issuer/audience URLs
- CORS origins

## Examples

### E-commerce Platform

```powershell
dotnet new vsclean `
  -n ShopNow.Catalog `
  --ClientName ShopNow `
  --ProjectSuffix Catalog `
  --DatabaseName ShopNowCatalog `
  --AdminEmail admin@shopnow.com
```

### Healthcare System

```powershell
dotnet new vsclean `
  -n MedTech.Patient `
  --ClientName MedTech `
  --ProjectSuffix Patient `
  --DatabaseName MedTechPatients `
  --AdminEmail admin@medtech.health
```

### Financial Services

```powershell
dotnet new vsclean `
  -n FinServ.Transactions `
  --ClientName FinServ `
  --ProjectSuffix Transactions `
  --DatabaseName FinServTxDb `
  --AdminEmail admin@finserv.io
```

## Publishing Template to NuGet (Optional)

To share this template with your team:

### 1. Create NuGet Package

```powershell
# Pack the template
dotnet pack -o ./nupkg

# Publish to NuGet.org or private feed
dotnet nuget push ./nupkg/CleanVerticalSlice.Template.1.0.0.nupkg -s https://api.nuget.org/v3/index.json -k [YOUR_API_KEY]
```

### 2. Team Installation

```powershell
# Install from NuGet
dotnet new install CleanVerticalSlice.Template

# Or from private feed
dotnet new install CleanVerticalSlice.Template --nuget-source https://your-private-feed.com/nuget
```

## Troubleshooting

### Template Not Found

```powershell
# Verify installation
dotnet new list

# Reinstall if needed
dotnet new install [path-to-template]
```

### Name Replacement Issues

- Ensure `ClientName` doesn't contain special characters
- Use PascalCase for names (e.g., `MyCompany`, not `my-company`)
- Avoid reserved C# keywords

### Build Errors After Creation

```powershell
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

## Best Practices

1. **Always change default admin password** after first login
2. **Generate new JWT secret** for each project (don't reuse from template)
3. **Update CORS origins** to match your frontend URLs
4. **Review security settings** in `SECURITY.md`
5. **Run migrations** before first application start
6. **Configure CI/CD** for automated deployments

## Support

For issues or questions about the template:
- Review `README.md` in generated project
- Check `SECURITY.md` for security guidelines
- Review ADR docs in `docs/adr/` for architectural decisions
