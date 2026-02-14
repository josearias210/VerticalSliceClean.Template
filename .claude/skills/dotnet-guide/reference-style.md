# Coding Style Reference

## File-Scoped Namespaces

**Mandatory.** Every `.cs` file uses file-scoped namespace as the first meaningful line. The namespace mirrors the folder path exactly:

```csharp
// File: src/Acme.Application/Features/Account/RegisterAccount/RegisterAccountCommand.cs
namespace Acme.Application.Features.Account.RegisterAccount;

public class RegisterAccountCommand : IRequest<ErrorOr<Unit>>
{
    public required string FirstName { get; set; }
}
```

```csharp
// File: src/Acme.Domain/Entities/Account.cs
namespace Acme.Domain.Entities;

public class Account : IdentityUser { ... }
```

```csharp
// File: src/Acme.Infrastructure/Persistence/EF/ApplicationDbContext.cs
namespace Acme.Infrastructure.Persistence.EF;

public class ApplicationDbContext(...) : IdentityDbContext<Account>(options) { ... }
```

```csharp
// File: src/Acme.Api/Endpoints/AccountsEndpoints.cs
namespace Acme.Api.Endpoints;

public sealed class AccountsEndpoints : IEndpoint { ... }
```

```csharp
// File: tests/Acme.Application.Unit.Tests/Features/Account/RegisterAccount/RegisterAccountCommandValidatorTests.cs
namespace Acme.Application.Unit.Tests.Features.Account.RegisterAccount;

public class RegisterAccountCommandValidatorTests { ... }
```

**Never** use block-scoped namespaces:
```csharp
// WRONG
namespace Acme.Domain.Entities
{
    public class Account { ... }
}
```

## Primary Constructors (C# 12)

Preferred for dependency injection in handlers, services, and middleware:

```csharp
// CORRECT: Primary constructor
public class RegisterAccountCommandHandler(
    UserManager<Account> userManager,
    IEmailService emailService,
    ILogger<RegisterAccountCommandHandler> logger) : IRequestHandler<RegisterAccountCommand, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(...) { ... }
}

// CORRECT: Primary constructor for middleware
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context) { ... }
}

// CORRECT: Primary constructor for DbContext
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<Account>(options), IApplicationDbContext
{ ... }
```

## Sealed Classes

Use `sealed` on classes not designed for inheritance:

```csharp
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account> { ... }
public sealed class AccountsEndpoints : IEndpoint { ... }
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<...> { ... }
internal sealed class GlobalExceptionHandler : IExceptionHandler { ... }
```

## Null Handling

```csharp
// Null-conditional operator
public string? UserName => httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;

// Null coalescing
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

// Pattern matching (preferred over != null)
if (user is not null)
{
    return Error.Conflict(ErrorCodes.Account.EmailExists);
}

// Required keyword for non-nullable properties
public required string Email { get; set; }
public required string FullName { get; set; }
```

## Switch Expressions

Preferred over traditional switch statements:

```csharp
// Enum conversion
public static string ToRoleName(this Role role) =>
    role switch
    {
        Role.Developer => Roles.Developer,
        Role.Admin => Roles.Admin,
        Role.User => Roles.User,
        Role.Manager => Roles.Manager,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
    };

// Exception mapping
var (status, title, detail, logLevel) = exception switch
{
    ArgumentNullException argNull => (
        StatusCodes.Status400BadRequest,
        "Bad Request",
        $"Required parameter '{argNull.ParamName}' is missing.",
        LogLevel.Warning),
    DbUpdateConcurrencyException => (
        StatusCodes.Status409Conflict,
        "Concurrency Conflict",
        "The resource was modified by another user.",
        LogLevel.Warning),
    _ => (
        StatusCodes.Status500InternalServerError,
        "Internal Server Error",
        "An unexpected error occurred.",
        LogLevel.Error)
};
```

## Expression-Bodied Members

Use for simple single-expression properties and methods:

```csharp
// Properties
public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
public DbSet<Account> Accounts => Set<Account>();

// Methods
public static string ToRoleName(this Role role) => role switch { ... };
```

## String Handling

```csharp
// Interpolation
logger.LogInformation("Registering account for {Email}", command.Email);

// Raw string literals for multi-line
logger.LogInformation(
    """
    ===============================================
    WELCOME - YOUR ACCOUNT IS READY
    ===============================================
    To: {Email}
    Temporary Password: {Password}
    ===============================================
    """,
    email, temporaryPassword);
```

## Structured Logging

Always use property placeholders, never concatenation:

```csharp
// CORRECT
logger.LogInformation("Account created for {Email} with role {Role}", command.Email, roleName);
logger.LogWarning("The {Email} is already in use", command.Email);
logger.LogError(ex, "Failed to send welcome email to {Email}", command.Email);

// WRONG - never do this
logger.LogInformation("Account created for " + command.Email);
logger.LogInformation($"Account created for {command.Email}");
```

## Async Patterns

```csharp
// Always pass CancellationToken
public async Task<ErrorOr<Unit>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
{
    await userManager.CreateAsync(account, password);
    await emailService.SendWelcomeWithPasswordAsync(email, password, cancellationToken);
    return Unit.Value;
}

// Default parameter for optional cancellation
public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
{
    var pending = await applicationDbContext.Database.GetPendingMigrationsAsync(cancellationToken);
    if (pending.Any())
    {
        await applicationDbContext.Database.MigrateAsync(cancellationToken);
    }
}
```

## Extension Method Pattern

```csharp
// Service registration — chainable, returns IServiceCollection
public static IServiceCollection AddPresentation(this IServiceCollection services)
{
    services.AddOpenApi();
    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddEndpoints();
    return services;
}

// Middleware — chainable, returns IApplicationBuilder
public static IApplicationBuilder UseDefaultSecurityHeaders(
    this IApplicationBuilder app, IHostEnvironment env)
{
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers.XContentTypeOptions = "nosniff";
        await next();
    });
    return app;
}

// Domain — descriptive verb
public static string ToRoleName(this Role role) => ...;
public static Role ToRole(this string roleName) => ...;

// ErrorOr result mapping
public static Results<Ok<TValue>, ProblemHttpResult, NotFound, Conflict>
    ToTypedResult<TValue>(this ErrorOr<TValue> result) => ...;
```

## Entity Structure

```csharp
// Domain entity — extends Identity when needed
public class Account : IdentityUser
{
    public string? FullName { get; set; }
    public string? PreferredUsername { get; set; }
}

// EF Configuration — separate file, sealed
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.PreferredUsername).HasMaxLength(50);
        builder.HasIndex(x => x.PreferredUsername)
            .HasDatabaseName("IX_Accounts_PreferredUsername");
    }
}
```

## Constants and Enums

```csharp
// Enum with explicit values
public enum Role
{
    Developer = 1,
    Admin = 2,
    User = 3,
    Manager = 4
}

// String constants matching enum — used with Identity framework
public static class Roles
{
    public const string Developer = nameof(Developer);
    public const string Admin = nameof(Admin);
    public const string User = nameof(User);
    public const string Manager = nameof(Manager);
}
```

## Settings Classes

```csharp
// Strongly-typed with validation annotations
public class OpenTelemetrySettings
{
    [Required(ErrorMessage = "OTLP Endpoint is required")]
    [Url(ErrorMessage = "OTLP Endpoint must be a valid URL")]
    public required string OtlpEndpoint { get; set; }

    [Range(512, 10000)]
    public int MaxQueueSize { get; set; } = 2048;
}

// Registration with fail-fast validation
services.AddOptions<OpenTelemetrySettings>()
    .BindConfiguration("OpenTelemetry")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Endpoint Response Patterns

```csharp
// Standard CRUD result mapping
accounts.MapPost("/", async (ISender sender, [FromBody] CreateCommand cmd, CancellationToken ct) =>
    (await sender.Send(cmd, ct)).ToCreatedResult())
    .RequireAuthorization(p => p.RequireRole(Roles.Admin));

accounts.MapGet("/{id}", async (ISender sender, Guid id, CancellationToken ct) =>
    (await sender.Send(new GetQuery { Id = id }, ct)).ToTypedResult());

accounts.MapDelete("/{id}", async (ISender sender, Guid id, CancellationToken ct) =>
    (await sender.Send(new DeleteCommand { Id = id }, ct)).ToNoContentResult());

// Auth endpoint (returns Unauthorized variant)
auth.MapPost("/token", async (ISender sender, [FromBody] LoginCommand cmd, CancellationToken ct) =>
    (await sender.Send(cmd, ct)).ToAuthResult());
```

## .editorconfig Rules

**File:** `.editorconfig`

| Rule | Value |
|------|-------|
| Charset | UTF-8 |
| Line endings | CRLF |
| Indent (default) | 2 spaces |
| Indent (C#) | 4 spaces |
| Final newline | Insert |
| Trailing whitespace | Trim |

### Disabled Analyzer Rules

| Rule | Reason |
|------|--------|
| SA1124 | Allow `#region` directives |
| SA1402 | Allow multiple types per file |
| SA1649 | File name doesn't need to match first type |
| S6964 | Disabled (SonarAnalyzer) |
| CA1848 | Allow non-static LoggerMessage delegates |
