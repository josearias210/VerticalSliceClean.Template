---
name: dotnet-guide
description: >
  Guide for building .NET applications following Clean Architecture + Vertical Slice patterns.
  Use this skill when creating new features, endpoints, handlers, validators, services,
  entities, tests, or any .NET code in this project. Also use when reviewing code for
  architectural compliance or asking about project conventions.
allowed-tools: Read, Grep, Glob
---

# .NET Development Guide — Clean Architecture + Vertical Slice

This skill defines the architecture, patterns, conventions, and coding style used in this project.
All new code MUST follow these guidelines. When in doubt, look at existing code for reference.

## Target Framework

This project targets **.NET 10** with **C# 14**. Always prefer the latest language features available:
- File-scoped namespaces (mandatory)
- Primary constructors for DI
- `required` keyword for mandatory properties
- Switch expressions over switch statements
- Pattern matching (`is not null`, `is`)
- Raw string literals for multi-line strings
- Records for immutable DTOs
- Target-typed `new` expressions

## File-Scoped Namespaces

**Every `.cs` file** uses file-scoped namespaces (single line, no braces). The namespace MUST be the first meaningful line after `using` directives:

```csharp
namespace Acme.Application.Features.Account.RegisterAccount;

public class RegisterAccountCommand : IRequest<ErrorOr<Unit>>
{
    // ...
}
```

Namespace mirrors the folder path exactly: `{Project}.{Folder}.{Subfolder}`.

## Architecture Overview

**Clean Architecture layers** with strict dependency flow:

```
Host → Api → Application → Domain
Host → Infrastructure → Application → Domain
```

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Domain** | `Acme.Domain` | Entities, enums, constants, extension methods. Zero external dependencies. |
| **Application** | `Acme.Application` | CQRS handlers, validators, pipeline behaviors, abstractions (interfaces). |
| **Infrastructure** | `Acme.Infrastructure` | EF Core, Identity, auth, external services, settings classes. |
| **Api** | `Acme.Api` | Minimal API endpoints (`IEndpoint`), exception handlers, result mapping. |
| **Host** | `Acme.Host` | `Program.cs`, middleware pipeline, DI composition, startup extensions. |

**Each layer** has a `DependencyInjection.cs` with an `Add{Layer}()` extension method. Composed in `Program.cs`:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddPresentation()
    .AddHost(builder.Configuration);
```

## Vertical Slice Pattern

Every feature is self-contained under `Acme.Application/Features/{Feature}/{Action}/`:

```
Features/
└── Account/
    └── RegisterAccount/
        ├── RegisterAccountCommand.cs          # IRequest<ErrorOr<T>>
        ├── RegisterAccountCommandHandler.cs   # IRequestHandler<,>
        └── RegisterAccountCommandValidator.cs # AbstractValidator<T>
```

Corresponding endpoint in `Acme.Api/Endpoints/{Feature}Endpoints.cs` implementing `IEndpoint`.

## CQRS Pattern (Command/Query)

For detailed implementation patterns with full code examples, see `reference-cqrs.md`.

**Command** — implements `IRequest<ErrorOr<T>>`, uses `required` keyword for mandatory fields, no logic.

**Handler** — implements `IRequestHandler<TRequest, ErrorOr<TResponse>>`, uses primary constructor for DI, returns `ErrorOr<T>` (never throws for business errors).

**Validator** — extends `AbstractValidator<T>`, every rule has `.WithErrorCode()` from `ErrorCodes`, stateless (registered as Singleton).

**Pipeline** — `ValidationBehavior` intercepts all requests, runs validators before handlers, converts failures to `ErrorOr` errors.

## Error Handling

For the complete error handling reference with ProblemDetails, see `reference-errors.md`.

### Two-Layer Strategy

**Layer 1 — Business errors** (expected): Use `ErrorOr<T>`, never throw.
**Layer 2 — Unexpected exceptions**: `GlobalExceptionHandler` → RFC 7807 ProblemDetails.

### ErrorOr in Handlers

```csharp
return Error.Conflict(ErrorCodes.Account.EmailExists);    // 409
return Error.Validation(code, description);                // 400
return Error.Failure(ErrorCodes.Account.CreateFailed);     // 400
return Error.NotFound();                                   // 404
return Error.Unauthorized();                               // 401
return Unit.Value;                                         // Success
```

### Error Codes

Defined in `Acme.Domain/Constants/ErrorCodes.cs`. Format: `{Domain}.{Property}.{ErrorType}`:

```csharp
public static class ErrorCodes
{
    public static class Account
    {
        public const string FirstNameEmpty = "Account.FirstName.Required";
        public const string EmailExists = "Account.EmailExists";
        public const string RoleInvalid = "Account.Role.Invalid";
    }
}
```

### ErrorOr → HTTP Result Mapping

Extension methods in `Acme.Api/Extensions/TypedResultsExtensions.cs` convert `ErrorOr<T>` to typed minimal API results:

| Method | Success | Use case |
|--------|---------|----------|
| `ToTypedResult()` | `200 OK` | GET, general |
| `ToCreatedResult(uri)` | `201 Created` | POST |
| `ToNoContentResult()` | `204 No Content` | DELETE, PUT |
| `ToAuthResult()` | `200 OK` | Login (adds 401 variant) |

All methods map `ErrorType.Validation` → `400`, `ErrorType.NotFound` → `404`, `ErrorType.Conflict` → `409`.

### ProblemDetails

All error HTTP responses use **RFC 7807 ProblemDetails** format. Customized to include:

```csharp
// ProblemDetailsExtensions.cs
ctx.ProblemDetails.Extensions["traceId"] = http.TraceIdentifier;
ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
ctx.ProblemDetails.Type ??= $"https://httpstatuses.com/{status}";
```

### GlobalExceptionHandler

`internal sealed class` implementing `IExceptionHandler`. Maps unhandled exceptions to ProblemDetails:

- `DbUpdateConcurrencyException` → 409 Conflict
- `ArgumentNullException` / `ArgumentException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- `UnauthorizedAccessException` → 403 Forbidden
- `TimeoutException` → 504 Gateway Timeout
- `OperationCanceledException` → 499 Client Closed
- Any other → 500 Internal Server Error

In development mode, extends ProblemDetails with `exceptionType` and `innerException`.

## Endpoint Pattern

**Every endpoint MUST have OpenAPI documentation.** No endpoint is complete without `.WithSummary()`, `.WithTags()`, and `.Produces()` declarations.

```csharp
public sealed class AccountsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("api/v1/accounts")
            .RequireAuthorization()
            .RequireRateLimiting("auth")
            .WithTags("Accounts");                          // Group tag for Scalar/Swagger UI

        accounts.MapPost("/", async (ISender sender, [FromBody] RegisterAccountCommand command, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToTypedResult())
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin))
            .WithName("RegisterAccount")                    // Operation ID
            .WithSummary("Register a new account")          // Short title in docs
            .WithDescription("Creates a new user account and sends a welcome email with a temporary password.")
            .Produces<AccountResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()                    // 400 - validation errors
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);       // 409 - email already exists
    }
}
```

### OpenAPI Rules

- **`.WithTags("...")`** — always on the group, not on individual endpoints
- **`.WithName("...")`** — PascalCase operation ID matching the action name (`RegisterAccount`, `GetAccountById`)
- **`.WithSummary("...")`** — short phrase (max ~10 words), no period at end
- **`.WithDescription("...")`** — full sentence explaining what the endpoint does and any side effects
- **`.Produces<T>(statusCode)`** — document every success response with its DTO type
- **`.ProducesValidationProblem()`** — shorthand for 400 with `ValidationProblemDetails`, use when validator exists
- **`.Produces(statusCode)`** — for non-body responses (401, 403, 404, 409)

### Produces by Result Method

| `ToXxxResult()` used | Required `.Produces()` declarations |
|---------------------|--------------------------------------|
| `ToTypedResult()` | `Produces<T>(200)`, `ProducesValidationProblem()`, `Produces(404)` if NotFound possible |
| `ToCreatedResult()` | `Produces<T>(201)`, `ProducesValidationProblem()`, `Produces(409)` if Conflict possible |
| `ToNoContentResult()` | `Produces(204)`, `ProducesValidationProblem()`, `Produces(404)` if NotFound possible |
| `ToAuthResult()` | `Produces<T>(200)`, `ProducesValidationProblem()`, `Produces(401)` |

Endpoints are auto-discovered via assembly scanning and registered as Singleton.

## Coding Style

For the complete coding style reference, see `reference-style.md`.

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Commands | `{Action}Command` | `RegisterAccountCommand` |
| Handlers | `{Action}CommandHandler` | `RegisterAccountCommandHandler` |
| Validators | `{Action}CommandValidator` | `RegisterAccountCommandValidator` |
| Services | `{Purpose}Service` or descriptive noun | `EmailService`, `PasswordGenerator` |
| EF Configurations | `{Entity}Configuration` | `AccountConfiguration` |
| Extensions | `{Purpose}Extensions` | `SecurityHeadersExtensions` |
| Middleware | `{Purpose}Middleware` | `CorrelationIdMiddleware` |
| Health Checks | `{Purpose}HealthCheck` | `DbContextConnectivityHealthCheck` |
| Error Codes | `{Domain}.{Property}.{ErrorType}` | `Account.FirstName.Required` |
| Async methods | `{Verb}Async` suffix | `SendWelcomeAsync`, `ApplyMigrationsAsync` |
| Interfaces | `I` prefix | `IApplicationDbContext`, `IEndpoint` |
| Private fields | `_camelCase` (or primary constructor params) | `_validator` |
| Constants | `PascalCase` | `MigrationsHistoryTable` |

### C# Style Preferences

- **Primary constructors** (C# 12) for DI injection in handlers, services, middleware
- **`required` keyword** for mandatory properties in commands/DTOs
- **`sealed`** on classes not designed for inheritance (configurations, endpoints, handlers)
- **Switch expressions** over switch statements for enum conversions and pattern matching
- **Null handling**: `?.` for navigation, `??` for defaults, `is not null` for checks
- **Expression-bodied members** for simple single-expression properties/methods
- **String interpolation** with `$""` and raw string literals `"""..."""` for multi-line
- **Structured logging**: `logger.LogInformation("Event for {Email}", email)` — never concatenation
- **Async/await** on all I/O; always pass `CancellationToken`

### DI Lifetime Rules

| Lifetime | When to use | Examples |
|----------|-------------|---------|
| **Singleton** | Stateless services, validators, endpoints, configuration | `IPasswordGenerator`, `IEndpoint`, validators |
| **Scoped** | Request-specific state, DbContext, handlers, pipeline behaviors | `ApplicationDbContext`, `IUserIdentityService`, `IEmailService` |
| **Transient** | Rarely used; only for stateful short-lived objects | — |

## Testing

For testing patterns with examples, see `reference-testing.md`.

- **Framework**: xUnit + FluentAssertions + Moq
- **Structure**: Mirrors source — `tests/.../Features/{Feature}/{Action}/`
- **Class naming**: `{TargetClass}Tests`
- **Method naming**: `{MethodUnderTest}{Scenario}{ExpectedResult}`
- **Pattern**: Arrange-Act-Assert
- **Assertions**: FluentAssertions (`.Should()`)
- **Parameterized**: `[Theory]` + `[InlineData]` for multiple cases
- **Validate against error codes**, not message strings

## Build Configuration (Directory.Build.props)

Applies to ALL projects in the solution. Settings defined once at root level:

```xml
<LangVersion>14.0</LangVersion>
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<AnalysisLevel>latest-recommended</AnalysisLevel>
```

- StyleCop analysis excludes EF migrations (`**/Migrations/*.cs`)
- All projects inherit these settings — never override in individual `.csproj`

## Package Management (Directory.Packages.props)

**Central Package Management** (`ManagePackageVersionsCentrally=true`):

- ALL NuGet versions defined in `Directory.Packages.props` at repo root
- Individual `.csproj` files reference packages **WITHOUT version numbers**
- To add a package: add `<PackageVersion>` to `Directory.Packages.props`, then `<PackageReference>` (no version) to `.csproj`
- To update a version: change ONLY `Directory.Packages.props`

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="MediatR" Version="13.1.0" />

<!-- .csproj — NO version here -->
<PackageReference Include="MediatR" />
```

## Configuration

- **Settings classes** with `[Required]`, `[Url]`, `[Range]` data annotations, `required` keyword
- **Registration**: `.AddOptions<T>().BindConfiguration("Section").ValidateDataAnnotations().ValidateOnStart()`
- **Fail-fast**: All settings validated at startup — app won't start with invalid config
- **Secrets**: User Secrets locally, environment variables in production

## Observability

For full configuration details, see `reference-infrastructure.md`.

- **Serilog**: Console (dev), Seq (structured), File (persistent + error-only). Enriched with TraceId, SpanId, MachineName, ThreadId. Microsoft loggers overridden to Warning.
- **OpenTelemetry**: Tracing (ASP.NET Core, HttpClient, EF Core) + Metrics (Runtime, ASP.NET, HttpClient). OTLP gRPC export to Jaeger. Batch export with configurable queue/timeout.
- **Correlation ID**: `X-Correlation-Id` header middleware generates/propagates ID, links to OpenTelemetry Activity.
- **Health Checks**: `/health/live` (liveness), `/health/ready` (readiness), `/health` (full). DB connectivity, migrations, memory, startup checks.

## Authentication & Authorization

For full details, see `reference-auth.md`.

- **OpenIddict**: OAuth2 server at `/connect/token` and `/connect/revoke`. Password + Refresh Token flows. Scopes: OpenId, Profile, Email, api.
- **Token lifetimes**: Access 15min, Refresh 14 days.
- **Certificates**: Ephemeral in development, file-based PKCS12 in production.
- **Identity**: Password policy (8+ chars, upper+lower+digit+special), lockout (5 attempts → 30 min).
- **Policies**: `AdminOnly`, `CanManageUsers`, `CanManageProducts`, `EmailVerified`, `MfaEnabled`, `RequireApiScope`, `RequireAdminScope`.

## Database Management

For full details, see `reference-database.md`.

- **PostgreSQL + Npgsql**: Retry policy (3 retries, 5s), 30s command timeout. Sensitive logging in dev only.
- **DbContext Factory**: Design-time factory for `dotnet ef migrations`. Loads User Secrets automatically.
- **Migrations**: Extended 5-min timeout. Run from `src/Acme.Infrastructure` with `--startup-project ../Acme.Host`.
- **Seeding**: Roles from enum, Admin user with 12-char password validation + rollback, OpenIddict `react-app` public client.
- **Startup**: `InitializeDatabaseAsync()` runs before pipeline. Fail-fast on error. Controlled by `DatabaseSettings` flags.

## Rate Limiting

- **`"auth"`**: 5 requests / 15 min, no queue (login/register endpoints)
- **`"general"`**: 100 requests / 1 min, queue 5 (general endpoints)
- **`"per-ip"`**: 10 concurrent, queue 2 (abuse prevention)
- Rejection returns 429 with retry-after. Skipped in test environment.

## Security

- **Headers**: X-Content-Type-Options: nosniff, X-Frame-Options: DENY, CSP, Referrer-Policy, HSTS (prod only). CSP skipped for /scalar and /openapi.
- **CORS**: Dynamic from `CorsSettings`. Wildcard origin disables credentials. Safe deny-all default if no origins.
- **JSON**: Null values omitted, enums as strings.

## Docker & Deployment

For full details, see `reference-deployment.md`.

- **Dockerfile**: Multi-stage (SDK build → aspnet runtime). Non-root `appuser`. Health check via curl. Ports 8080/8081.
- **Local**: `docker compose -f docker-compose.local.yml up -d` → API + PostgreSQL + Jaeger + Seq.
- **Production**: + Caddy reverse proxy (auto SSL), resource limits, backup sidecar.
- **Bootstrap order**: Serilog → DI layers → Build → SerilogRequestLogging → InitializeDatabase → ConfigurePipeline → Run.

## Folder Structure Reference

```
src/Acme.Domain/
├── Entities/          Constants/          Enums/          Extensions/

src/Acme.Application/
├── Abstractions/      Behaviors/          Common/
└── Features/{Feature}/{Action}/

src/Acme.Infrastructure/
├── Auth/              Extensions/         HealthChecks/
├── Persistence/EF/    Services/           Settings/

src/Acme.Api/
├── Endpoints/         Exceptions/         Extensions/

src/Acme.Host/
├── Extensions/        Middleware/          Properties/

tests/Acme.Application.Unit.Tests/
├── Features/{Feature}/{Action}/    Behaviors/    Domain/
```

## Creating a New Feature (Checklist)

1. Create folder: `src/Acme.Application/Features/{Feature}/{Action}/`
2. Create command/query: `{Action}Command.cs` implementing `IRequest<ErrorOr<T>>`
3. Create handler: `{Action}CommandHandler.cs` implementing `IRequestHandler<,>`
4. Create validator: `{Action}CommandValidator.cs` extending `AbstractValidator<T>`
5. Add error codes to `Acme.Domain/Constants/ErrorCodes.cs` if needed
6. Create endpoint: `src/Acme.Api/Endpoints/{Feature}Endpoints.cs` implementing `IEndpoint`
7. Add unit tests: `tests/.../Features/{Feature}/{Action}/{Action}CommandValidatorTests.cs`
8. All auto-discovered — no manual registration needed
