# VerticalSliceClean API Template

> **🎯 .NET 10 API Template** with Clean Architecture and Vertical Slice patterns for rapid development with enterprise-grade features.

This is a **production-ready project template** (`dotnet new`) for building RESTful APIs with **JWT authentication**, **httpOnly cookies**, **Vertical Slice Architecture**, and **ErrorOr pattern** for functional error handling.

Built with **.NET 10**, **EF Core**, **MediatR**, **FluentValidation**, **Serilog**, and **OpenTelemetry**.

---

## 🚀 Quick Start with Template

### Install Template
```powershell
# From source (recommended for now)
git clone https://github.com/josearias210/VerticalSliceClean.Template.git
cd VerticalSliceClean.Template
dotnet new install .

# Verify installation
dotnet new list vsclean
```

### Create New Project
```powershell
# Basic usage
dotnet new vsclean -n Acme.CRM --ClientName Acme --ProjectSuffix CRM

# Full customization
dotnet new vsclean -n Contoso.Sales `
  --ClientName Contoso `
  --ProjectSuffix Sales `
  --DatabaseName ContosoSalesDb `
  --AdminEmail admin@contoso.com `
  --ProductionDomain api.contoso.com `
  --GitHubUsername contosodev
```

📖 **[Full Template Usage Guide](.template.config/TEMPLATE_USAGE.md)**

---

## 🏗️ Architecture

### **Vertical Slice Architecture**
- Each feature is self-contained (Command/Query + Handler + Validator + Endpoint)
- No shared business logic layers
- Easy to add/remove features without side effects

### **Clean Architecture Layers**
```
┌─────────────────────────────────────────┐
│           [Client].AppHost              │  ← Entry point, middleware pipeline
├─────────────────────────────────────────┤
│            [Client].Api                 │  ← Endpoints, Swagger, versioning
├─────────────────────────────────────────┤
│         [Client].Application            │  ← CQRS handlers, validators
├─────────────────────────────────────────┤
│        [Client].Infrastructure          │  ← DbContext, auth, services
├─────────────────────────────────────────┤
│           [Client].Domain               │  ← Entities, enums
└─────────────────────────────────────────┘
```

### **Key Patterns**
- **CQRS** with MediatR
- **ErrorOr** for functional error handling
- **Typed Results** for compile-time type safety
- **Repository pattern avoided** (DbContext used directly in handlers)
- **FluentValidation** with pipeline behavior
- **Global Exception Handler** for unhandled errors

---

## ✨ Features

### **Security** 🔒
- ✅ JWT authentication with **httpOnly cookies** (refresh + access tokens)
- ✅ **Account Lockout** (5 failed attempts → 30 min lockout)
- ✅ **Token Reuse Detection** (revokes all tokens on reuse)
- ✅ **Auto-generated passwords** with complexity requirements
- ✅ **Security Headers** (CSP, HSTS, X-Frame-Options, etc.)
- ✅ **Rate Limiting** per endpoint
- ✅ **CORS** configured for development

### **Observability** 📊
- ✅ **Structured Logging** with Serilog (Console + File)
- ✅ **OpenTelemetry** distributed tracing
- ✅ **Jaeger** UI for traces
- ✅ **CorrelationId** middleware for request tracking
- ✅ **Health Checks** for DB and app

### **API** 🌐
- ✅ **Swagger** + **Scalar** API documentation
- ✅ **API Versioning** (URL segment based: `/api/v1/...`)
- ✅ **ProblemDetails** (RFC 7807) with traceId/correlationId
- ✅ **Typed Results** for better OpenAPI generation

### **Database** 💾
- ✅ **EF Core 10** with SQL Server
- ✅ **Migrations** with distributed locking (sp_getapplock)
- ✅ **Auto-creation** of database on first run
- ✅ **Soft Delete** with query filters
- ✅ **Retry Policy** for transient failures
- ✅ **Background Job** for token cleanup (runs daily at 3 AM)

---

## 🚀 Getting Started

> **Note:** This is a **template repository**. For production use, create a new project using `dotnet new vsclean`. See [Template Usage Guide](.template.config/TEMPLATE_USAGE.md).

### **Prerequisites**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for SQL Server, Jaeger, Seq)
- [Git](https://git-scm.com/)

### **1. Clone or Use Template**
```bash
# Option A: Use as template (recommended)
dotnet new cleanslice -n YourCompany.YourProject

# Option B: Clone this repository (for development/customization)
git clone https://github.com/josearias210/VerticalSliceClean.Template.git
cd VerticalSliceClean.Template
```

### **2. Start Infrastructure (Docker Compose)**

**Opción A: Script automatizado (recomendado)**
```powershell
# Inicia Docker Compose y verifica servicios
.\setup-dev.ps1
```

**Opción B: Manual**
```powershell
# Inicia: SQL Server + Jaeger (tracing) + Seq (logs)
docker compose up -d

# Verifica que los servicios estén corriendo
docker compose ps
```

**Servicios disponibles:**
- **SQL Server**: `localhost:1433` (sa / Local123*)
- **Jaeger UI**: http://localhost:16686 (distributed tracing)
- **Seq UI**: http://localhost:5341 (structured logs, admin / Admin123!)

📖 **[Guía completa de Docker Compose](docs/DOCKER_LOCAL.md)** - Comandos, troubleshooting, backups

```powershell
# Para detener los servicios
docker compose down

# Para detener y eliminar volúmenes (limpieza completa)
docker compose down -v
```

### **3. Configure User Secrets**

```powershell
cd src/[YourProject].Infrastructure

# JWT Settings (requerido)
dotnet user-secrets set "JwtSettings:Key" "YourSuperSecretKeyThatIsAtLeast32CharactersLong1234567890"
dotnet user-secrets set "JwtSettings:Issuer" "https://yourcompany.com"
dotnet user-secrets set "JwtSettings:Audience" "https://yourcompany.com"

# Admin User (requerido)
dotnet user-secrets set "Admin:Email" "admin@yourcompany.com"
dotnet user-secrets set "Admin:Password" "Admin@123456"

# Connection String (usa password de .env)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=YourProjectDb;User Id=sa;Password=Local123*;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

> 💡 **Tip**: El password `Local123*` debe coincidir con `MSSQL_SA_PASSWORD` en `.env`

### **4. Run the Application**

**Desde Visual Studio:**
1. Set `[YourProject].AppHost` como startup project
2. Press `F5`

**Desde terminal:**
```powershell
cd src/[YourProject].AppHost
dotnet run
```

La aplicación automáticamente:
1. ✅ Crea la base de datos si no existe
2. ✅ Aplica migraciones pendientes
3. ✅ Crea roles (Admin, Manager, ProductManager, User)
4. ✅ Crea el usuario administrador
5. ✅ Inicia en `http://localhost:5005`

> ⚠️ **Primera vez**: Si generaste el proyecto desde el template, debes crear la migración inicial primero. Ver [docs/GETTING_STARTED.md](docs/GETTING_STARTED.md)

### **5. Access the API**

- **Swagger UI:** http://localhost:7297/swagger
- **Scalar UI:** http://localhost:7297/scalar  
- **Health Check:** http://localhost:7297/health
- **Jaeger Tracing:** http://localhost:16686
- **Seq Logs:** http://localhost:5341

---

## 📚 API Endpoints

### **Authentication**
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/v1/accounts/login` | Login with email/password | ❌ |
| POST | `/api/v1/accounts/refresh` | Refresh access token | ❌ |
| POST | `/api/v1/accounts/logout` | Logout and revoke tokens | ✅ |
| GET | `/api/v1/accounts/me` | Get current user profile | ✅ |
| POST | `/api/v1/accounts/` | Register new account (Admin only) | ✅ |

### **Your Domain Endpoints**
Add your business logic endpoints here after creating your domain entities.

See [docs/MIGRATIONS.md](docs/MIGRATIONS.md) for guide on adding entities and creating migrations.

---

## 🔑 Default Credentials

After seeding, you can login with:

**Admin Account:**
- Email: `admin@yourcompany.com` (configured in User Secrets)
- Password: `Admin@123456` (configured in User Secrets)

> ⚠️ **Security:** Change default credentials after first login in production!

**Roles:**
- `Admin` - Full access
- `Manager` - Management access
- `ProductManager` - Can create/manage products
- `User` - Basic access

---

## 🚀 Production Deployment

### **VPS Deployment (Recommended)**

Deploy to DigitalOcean, Hetzner, or any VPS with Docker:

```bash
# Quick setup (5 minutes)
# 1. Configure GitHub Secrets (see docs/DEPLOYMENT_QUICKSTART.md)
# 2. Push to main branch
git push origin main

# GitHub Actions will automatically:
# ✅ Build & test
# ✅ Build Docker image
# ✅ Deploy to VPS via SSH
# ✅ Setup HTTPS with Caddy
```

**What you get:**
- ✅ Auto-deployments on push to main
- ✅ Automatic HTTPS (Let's Encrypt via Caddy)
- ✅ Database backups before each deploy
- ✅ Health checks & auto-restart
- ✅ Production-optimized Docker images

**Cost:** ~$24/month (2 vCPU / 4 GB VPS)

📖 **[Quick Start Guide](docs/DEPLOYMENT_QUICKSTART.md)** | **[Full Deployment Guide](docs/DEPLOYMENT.md)**

---

## 🛠️ Development

### **Project Structure**
```
[YourProject]/
├── src/
│   ├── [Client].[Project].AppHost/       # Entry point, middleware
│   ├── [Client].[Project].Api/           # Endpoints, Swagger
│   ├── [Client].[Project].Application/   # CQRS, validators
│   ├── [Client].[Project].Infrastructure/# DbContext, services
│   └── [Client].[Project].Domain/        # Entities, enums
├── docker-compose.yml               # SQL Server + Jaeger
└── README.md
```

### **Adding a new feature (Vertical Slice)**

The template includes authentication features. Add your domain features following this pattern:

**Example: Adding TodoItems**

1. Create entity in `Domain/Entities/TodoItem.cs`
2. Add DbSet to `Infrastructure/Persistence/EF/ApplicationDbContext.cs`
3. Create EF configuration (optional) in `Infrastructure/Persistence/EF/Configurations/`
4. Create migration: `dotnet ef migrations add AddTodoItems`
5. Create feature folder in `Application/Features/TodoItems/`
6. Add Command/Query + Handler + Validator
7. Add endpoint in `Api/Endpoints/TodoItemsEndpoints.cs`

**Complete Example:**
```csharp
// 1. Domain/Entities/TodoItem.cs
public sealed class TodoItem
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 2. Infrastructure/Persistence/EF/ApplicationDbContext.cs
public DbSet<TodoItem> TodoItems => Set<TodoItem>();

// 3. Application/Features/TodoItems/CreateTodoItem/CreateTodoItemCommand.cs
public record CreateTodoItemCommand(string Title) : IRequest<ErrorOr<TodoItem>>;

// 4. Application/Features/TodoItems/CreateTodoItem/CreateTodoItemCommandHandler.cs
public class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, ErrorOr<TodoItem>>
{
    public async Task<ErrorOr<TodoItem>> Handle(CreateTodoItemCommand request, CancellationToken ct)
    {
        var todoItem = new TodoItem 
        { 
            Title = request.Title, 
            CreatedAt = DateTime.UtcNow 
        };
        
        _dbContext.TodoItems.Add(todoItem);
        await _dbContext.SaveChangesAsync(ct);
        
        return todoItem;
    }
}

// 5. Api/Endpoints/TodoItemsEndpoints.cs
public class TodoItemsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("/api/v1/todos");

        todos.MapPost("/", async (ISender sender, CreateTodoItemCommand cmd, CancellationToken ct) =>
            (await sender.Send(cmd, ct)).ToCreatedResult("/api/v1/todos"))
        .WithMetadata("Create TodoItem", "Creates a new todo item")
        .RequireAuthorization();
    }
}
```

📖 **See [docs/MIGRATIONS.md](docs/MIGRATIONS.md) for complete guide**

### **Running migrations**
```bash
# Add new migration
cd src/[YourProject].Infrastructure
dotnet ef migrations add MigrationName --startup-project ../[YourProject].AppHost

# Update database
dotnet ef database update --startup-project ../[YourProject].AppHost
```

---

## 🔒 Security Features

See [SECURITY.md](SECURITY.md) for detailed security documentation:
- Token-based authentication flow
- Account lockout strategy
- Token reuse detection
- Password generation
- Rate limiting policies
- CORS configuration

---

## 📖 Architecture Decision Records

See [docs/adr/](docs/adr/) for architectural decisions:
- ADR-001: Vertical Slice Architecture
- ADR-002: ErrorOr Pattern
- ADR-003: Typed Results
- ADR-004: JWT with httpOnly Cookies
- ADR-005: API Versioning Strategy

---

## 📦 Template Distribution

This repository serves as a **dotnet new template**. You can:

### Local Installation (Recommended for Development)
```powershell
dotnet new install .
```

### Share with Team
- **Option 1:** Share repository URL for `git clone` + `dotnet new install`
- **Option 2:** Publish to private NuGet feed (Azure Artifacts, GitHub Packages)
- **Option 3:** Publish to NuGet.org for public use

📖 **[Publishing Guide](.template.config/PUBLISHING.md)**

---

## 📄 License

This project is licensed under the MIT License.

---

## 📞 Contact

**Jose Antonio Arias**
- Website: [programemos.net](https://programemos.net)
- GitHub: [@josearias210](https://github.com/josearias210)

---

## 🙏 Acknowledgments

- [ErrorOr](https://github.com/amantinband/error-or) by Amichai Mantinband
- [MediatR](https://github.com/jbogard/MediatR) by Jimmy Bogard
- [FluentValidation](https://github.com/FluentValidation/FluentValidation)
- [Serilog](https://serilog.net/)
- [Scalar](https://github.com/scalar/scalar) - Modern API documentation

