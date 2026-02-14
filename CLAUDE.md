# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

.NET 10 REST API template implementing **Clean Architecture + Vertical Slice** patterns. Uses PostgreSQL 16, MediatR (CQRS), FluentValidation, ErrorOr for functional error handling, OpenIddict for OAuth2/JWT auth, Serilog + OpenTelemetry for observability.

## Build & Run Commands

```bash
# Restore and build
dotnet restore src/Acme.slnx
dotnet build src/Acme.slnx --configuration Release --no-restore

# Run full local stack (API + Postgres + Jaeger + Seq)
docker compose -f docker-compose.local.yml up -d
# API: http://localhost:8080/swagger | Seq: http://localhost:5341 | Jaeger: http://localhost:16686

# Run infrastructure only, then API from source
docker compose -f docker-compose.local.yml up postgres seq jaeger -d
cd src/Acme.Host && dotnet run

# Run all unit tests
dotnet test tests/Acme.Application.Unit.Tests/Acme.Application.Unit.Tests.csproj --configuration Release

# Run a single test by name
dotnet test tests/Acme.Application.Unit.Tests/Acme.Application.Unit.Tests.csproj --filter "FullyQualifiedName~RegisterAccountCommandValidatorTests"

# EF Core migrations (run from src/Acme.Infrastructure)
dotnet ef migrations add <MigrationName> --startup-project ../Acme.Host
dotnet ef database update --startup-project ../Acme.Host
```

## Architecture

```
src/
├── Acme.Host            # Entry point, Program.cs, middleware pipeline composition
├── Acme.Api             # Minimal API endpoints (IEndpoint), OpenAPI, error mapping
├── Acme.Application     # CQRS handlers, validators, abstractions (no framework deps)
├── Acme.Infrastructure  # EF Core DbContext, Identity, auth, external services
└── Acme.Domain          # Entities, enums, constants, error codes
tests/
└── Acme.Application.Unit.Tests  # xUnit + FluentAssertions + Moq
```

**Dependency flow:** Host → Api → Application → Domain; Host → Infrastructure → Application → Domain

### Vertical Slice Pattern

Each feature is self-contained under `Acme.Application/Features/{Feature}/{Action}/`:
- `{Action}Command.cs` or `{Action}Query.cs` — implements `IRequest<ErrorOr<T>>`
- `{Action}CommandHandler.cs` — implements `IRequestHandler<TRequest, ErrorOr<TResponse>>`
- `{Action}CommandValidator.cs` — extends `AbstractValidator<T>` with error codes from `ErrorCodes`

Corresponding endpoint lives in `Acme.Api/Endpoints/{Feature}Endpoints.cs` implementing `IEndpoint`. Endpoints are auto-discovered and registered.

### Key Patterns

- **ErrorOr<T>** — all handlers return `ErrorOr<T>`, never throw for business errors
- **ValidationBehavior** — MediatR pipeline behavior runs all FluentValidation validators before the handler; failures become `ErrorOr` validation errors
- **DependencyInjection.cs** — each layer has one; composed via `AddApplication()`, `AddInfrastructure()`, `AddPresentation()`, `AddHost()` in Program.cs
- **Error codes** — defined as constants in `Acme.Domain/Constants/ErrorCodes.cs`, format: `Category.Subcategory.Reason`

### Middleware Pipeline Order

Exception handler → Status code pages → Correlation ID → CORS → Rate limiting → Authentication → Authorization → Security headers → Health checks → Endpoints

## Testing Conventions

- **Framework:** xUnit 2.9.3, FluentAssertions 8.8.0, Moq 4.20.72
- **Structure mirrors source:** `tests/.../Features/{Feature}/{Action}/`
- Use `[Fact]` for single cases, `[Theory]` with `[InlineData]` for parameterized
- Assert with FluentAssertions (`.Should()`)
- Validate against error codes, not error message strings

## Package Management

NuGet versions are centralized in `Directory.Packages.props` at the repo root. Individual `.csproj` files reference packages **without version numbers**. Always update versions in `Directory.Packages.props`.

## Build Configuration

`Directory.Build.props`: C# 14, .NET 10, nullable enabled, implicit usings, latest-recommended analyzers. StyleCop analysis excludes EF migrations.

## Environment Configuration

- Copy `.env.example` → `.env` for local development
- User secrets template: `secrets.json.example`
- Database seeding/migration flags in `appsettings.json` under `Database` section
- JWT secrets must be 32+ characters in production
