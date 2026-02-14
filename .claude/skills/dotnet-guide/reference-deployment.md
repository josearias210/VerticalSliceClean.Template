# Deployment Reference

## Dockerfile (Multi-Stage)

**File:** `src/Acme.Host/Dockerfile`

### Stage 1: Build

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first (layer caching)
COPY ["src/Acme.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Acme.Domain/Acme.Domain.csproj", "Acme.Domain/"]
COPY ["src/Acme.Application/Acme.Application.csproj", "Acme.Application/"]
COPY ["src/Acme.Infrastructure/Acme.Infrastructure.csproj", "Acme.Infrastructure/"]
COPY ["src/Acme.Api/Acme.Api.csproj", "Acme.Api/"]
COPY ["src/Acme.Host/Acme.Host.csproj", "Acme.Host/"]

RUN dotnet restore "Acme.Host/Acme.Host.csproj"
COPY src/ .

WORKDIR "/src/Acme.Host"
RUN dotnet publish "Acme.Host.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false
```

### Stage 2: Runtime

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
COPY --from=build /app/publish .
RUN mkdir -p /app/logs && chown -R appuser:appuser /app
USER appuser

EXPOSE 8080 8081

HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080;http://+:8081 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "Acme.Host.dll"]
```

Key patterns:
- **Layer caching**: Copy `.csproj` files first, restore, then copy source
- **Non-root user**: `appuser` for security
- **Health check**: Curl to `/health` endpoint
- **Two ports**: 8080 (API), 8081 (metrics)
- **Logs directory**: Created with proper ownership

---

## Docker Compose — Local Development

**File:** `docker-compose.local.yml`

### Services

| Service | Image | Ports | Purpose |
|---------|-------|-------|---------|
| `api` | Built from Dockerfile | 8080, 8081 | The .NET API |
| `postgres` | `postgres:16-alpine` | 5432 | Database |
| `jaeger` | `jaegertracing/jaeger:latest` | 16686 (UI), 4317 (OTLP gRPC), 4318 (OTLP HTTP) | Distributed tracing |
| `seq` | `datalust/seq:latest` | 5341 (UI) | Structured log aggregation |

### Dependency Order

```
api → depends_on: postgres (service_healthy), seq (service_started), jaeger (service_started)
```

### Access URLs (Local)

| Service | URL |
|---------|-----|
| API (Scalar docs) | http://localhost:8080/scalar |
| API (health) | http://localhost:8080/health |
| Seq UI | http://localhost:5341 |
| Jaeger UI | http://localhost:16686 |
| PostgreSQL | localhost:5432 |

### Commands

```bash
# Full stack
docker compose -f docker-compose.local.yml up -d

# Infrastructure only (run API from source)
docker compose -f docker-compose.local.yml up postgres seq jaeger -d
cd src/Acme.Host && dotnet run
```

---

## Docker Compose — Production

**File:** `docker-compose.yml`

Additional services vs local:
- **Caddy** reverse proxy with automatic SSL (ports 80, 443)
- **Database backup** sidecar
- **Resource limits** (CPU/memory constraints)
- **Named networks** (`acme-network`)

### Volumes

| Volume | Purpose |
|--------|---------|
| `postgres_data` | Database persistence |
| `postgres_backups` | Automated backups |
| `api_logs` | Application logs |
| `caddy_data` | SSL certificates |

---

## Environment Variables

### .env.example (Local Development)

```bash
API_PORT=8080
API_METRICS_PORT=8081
JWT_SECRET_KEY=SuperSecretKeyForDevelopmentOnly1234567890
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
DB_NAME=AcmeDb
JAEGER_UI_PORT=16686
JAEGER_OTLP_GRPC_PORT=4317
SEQ_UI_PORT=5341
SEQ_ADMIN_PASSWORD=Admin123!
```

### .env.production.example

```bash
DOCKER_IMAGE=ghcr.io/username/acme-api:latest
DOMAIN=api.yourdomain.com
DB_NAME=AcmeDb
POSTGRES_PASSWORD=YourStrongPassword123!
JWT_SECRET_KEY=CHANGE_TO_SECURE_KEY_32_CHARS_MIN
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=SecurePassword!
DB_APPLY_MIGRATIONS=true
DB_SEED_ROLES=true
DB_SEED_ADMIN=true
```

---

## appsettings Structure

### appsettings.json (Production Defaults)

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Warning",
            "Acme": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "Database": {
        "ApplyMigrationsOnStartup": false,
        "SeedRolesOnStartup": false,
        "SeedAdminOnStartup": false
    },
    "OpenIddict": {
        "EncryptionCertificatePath": null,
        "SigningCertificatePath": null,
        "CertificatePassword": null
    }
}
```

### appsettings.Development.json (Dev Overrides)

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Acme": "Debug"
        }
    },
    "Database": {
        "ApplyMigrationsOnStartup": true,
        "SeedRolesOnStartup": true,
        "SeedAdminOnStartup": true
    }
}
```

### Expected Configuration Sections

| Section | Settings Class | Source |
|---------|---------------|--------|
| `ConnectionStrings:DefaultConnection` | — | User Secrets / env var |
| `Database` | `DatabaseSettings` | appsettings |
| `Admin` | `AdminUserSettings` | User Secrets / env var |
| `JwtSettings` | `JwtSettings` | User Secrets / env var |
| `Logging:Seq` | `SeqSettings` | appsettings / env var |
| `OpenTelemetry` | `OpenTelemetrySettings` | appsettings / env var |
| `Cors` | `CorsSettings` | appsettings / env var |
| `OpenIddict` | `OpenIddictSettings` | appsettings / env var |

---

## Program.cs Bootstrap Order

**File:** `src/Acme.Host/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog
builder.Host.ConfigureSerilog();

// 2. Register services by layer
builder.Services
    .AddApplication()              // MediatR, validators, behaviors
    .AddInfrastructure(config, env) // DbContext, Identity, auth, OTEL, CORS, health checks
    .AddPresentation()             // Endpoints, exception handler, OpenAPI, JSON options
    .AddHost(config);              // Rate limiting

var app = builder.Build();

// 3. Serilog request logging
app.UseSerilogRequestLogging();

// 4. Initialize database (migrations + seeding)
await app.InitializeDatabaseAsync();

// 5. Configure middleware pipeline
app.ConfigurePipeline();

await app.RunAsync();
```

---

## Middleware Pipeline Order

**File:** `src/Acme.Host/Extensions/WebApplicationExtensions.cs`

```
Development only: MapOpenApi(), MapScalarApiReference("/scalar")
Production only:  UseHttpsRedirection(), UseHsts()

1.  UseExceptionHandler()           — Global exception → ProblemDetails
2.  UseStatusCodePages()            — 404, 405, etc.
3.  CorrelationIdMiddleware         — X-Correlation-Id header + Activity tag
4.  UseConfiguredCors()             — CORS (before auth)
5.  UseRateLimiter()                — Rate limiting (skipped in test env)
6.  UseAuthentication()             — OpenIddict JWT validation
7.  UseAuthorization()              — Policy evaluation
8.  UseDefaultSecurityHeaders()     — CSP, HSTS, X-Frame-Options, etc.
9.  MapDefaultHealthChecks()        — /health/live, /health/ready, /health
10. MapRoutes()                     — All IEndpoint implementations
11. ConfigureGracefulShutdown()     — ApplicationStopping/Stopped logging
```
