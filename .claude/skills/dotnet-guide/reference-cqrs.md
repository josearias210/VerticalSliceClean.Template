# CQRS Implementation Reference

## Command Definition

```csharp
public class RegisterAccountCommand : IRequest<ErrorOr<Unit>>
{
    public required string FirstName { get; set; }
    public required string Email { get; set; }
    public required Role Role { get; set; }
}
```

Rules:
- Implements `IRequest<ErrorOr<T>>` where `T` is the return type
- Use `Unit` for commands that don't return data
- Use `required` keyword for mandatory properties
- No logic, no methods — pure data transfer object

## Handler Implementation

```csharp
public class RegisterAccountCommandHandler(
    UserManager<Account> userManager,
    IEmailService emailService,
    IPasswordGenerator passwordGenerator,
    ILogger<RegisterAccountCommandHandler> logger) : IRequestHandler<RegisterAccountCommand, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is not null)
        {
            logger.LogWarning("The {Email} is already in use", command.Email);
            return Error.Conflict(ErrorCodes.Account.EmailExists);
        }

        var account = new Account
        {
            FullName = command.FirstName,
            Email = command.Email,
            UserName = command.Email,
            EmailConfirmed = true,
        };

        var temporaryPassword = passwordGenerator.GenerateStrong(16);
        var createResult = await userManager.CreateAsync(account, temporaryPassword);

        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create account for {Email}: {Errors}",
                command.Email,
                string.Join(", ", createResult.Errors.Select(e => e.Code)));
            return Error.Failure(ErrorCodes.Account.CreateFailed);
        }

        var roleName = command.Role.ToRoleName();
        await userManager.AddToRoleAsync(account, roleName);

        await emailService.SendWelcomeWithPasswordAsync(command.Email, temporaryPassword, cancellationToken);

        logger.LogInformation("Account created for {Email} with role {Role}", command.Email, roleName);
        return Unit.Value;
    }
}
```

Rules:
- Use **primary constructor** (C# 12) for dependency injection
- Return `ErrorOr<T>` — never throw for business errors
- Use structured logging with property placeholders `{PropertyName}`
- Always pass `CancellationToken` to async calls
- Return specific error types: `Error.Conflict()`, `Error.Failure()`, `Error.Validation()`, `Error.NotFound()`, `Error.Unauthorized()`
- Use error codes from `ErrorCodes` constants

## Validator Implementation

```csharp
public class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithErrorCode(Account.FirstNameEmpty);

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(Account.EmailEmpty);

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithErrorCode(Account.RoleEmpty);

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithErrorCode(Account.RoleInvalid);

        RuleFor(x => x.Role)
            .Must(role => role != Domain.Enums.Role.Developer)
            .WithErrorCode(Account.DeveloperRoleNotAllowed);
    }
}
```

Rules:
- Every rule MUST have `.WithErrorCode()` — never use default FluentValidation messages
- Error codes imported from `ErrorCodes.{Domain}` static class
- Stateless — no injected dependencies (registered as Singleton)
- Use `.Must()` for complex business rules
- One validator per command/query

## ValidationBehavior Pipeline

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var errors = failures.ConvertAll(f => Error.Validation(
            code: (f.ErrorCode.EndsWith("Validator", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(f.ErrorCode))
                ? f.PropertyName
                : f.ErrorCode,
            description: f.ErrorMessage));

        return (dynamic)errors;
    }
}
```

How it works:
1. Intercepts every MediatR request before the handler
2. Runs all registered validators for that request type in parallel
3. If no validation errors → passes to handler
4. If validation errors → converts to `ErrorOr` errors and short-circuits (handler never runs)
5. Prefers custom error codes; falls back to property name if error code is auto-generated

## Error Codes Pattern

```csharp
// Acme.Domain/Constants/ErrorCodes.cs
public static class ErrorCodes
{
    public static class Account
    {
        public const string FirstNameEmpty = "Account.FirstName.Required";
        public const string EmailEmpty = "Account.Email.Required";
        public const string PasswordEmpty = "Account.Password.Required";
        public const string RoleEmpty = "Account.Role.Required";
        public const string RoleInvalid = "Account.Role.Invalid";
        public const string DeveloperRoleNotAllowed = "Account.Role.DeveloperNotAllowed";
        public const string CreateFailed = "Account.CreateFailed";
        public const string EmailExists = "Account.EmailExists";
        public const string InsufficientPermissions = "Account.InsufficientPermissions";
    }
}
```

Convention: `{AggregateRoot}.{Property}.{ErrorType}` or `{AggregateRoot}.{ErrorDescription}`

## Registration (DependencyInjection.cs)

```csharp
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    var assembly = typeof(DependencyInjection).Assembly;

    services.AddValidatorsFromAssembly(assembly, lifetime: ServiceLifetime.Singleton);
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

    return services;
}
```

All handlers, validators, and behaviors are auto-discovered from the assembly.

## Endpoint Registration (Auto-Discovery)

**File:** `src/Acme.Api/Extensions/EndpointsExtensions.cs`

### IEndpoint Interface

```csharp
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

### Auto-Discovery Registration

```csharp
public static IServiceCollection AddEndpoints(this IServiceCollection services)
{
    var asm = typeof(EndpointsRegistration).Assembly;
    var endpointTypes = asm.DefinedTypes
        .Where(t => !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t));

    foreach (var type in endpointTypes)
    {
        services.AddSingleton(typeof(IEndpoint), type);
    }
    return services;
}
```

### Route Mapping

```csharp
public static IEndpointRouteBuilder MapRoutes(this IEndpointRouteBuilder app)
{
    var endpoints = app.ServiceProvider.GetServices<IEndpoint>();
    foreach (var endpoint in endpoints)
    {
        endpoint.Map(app);
    }
    return app;
}
```

How it works:
1. `AddEndpoints()` scans the Api assembly for all `IEndpoint` implementations
2. Registers them as Singleton in DI
3. `MapRoutes()` resolves all and calls `Map()` on each
4. **No manual registration needed** — just implement `IEndpoint` and it's auto-discovered
