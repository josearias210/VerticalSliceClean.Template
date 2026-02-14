# Error Handling & ProblemDetails Reference

## Two-Layer Error Strategy

### Layer 1: Business Errors — ErrorOr<T>

All CQRS handlers return `ErrorOr<T>`. Business errors are **never thrown as exceptions**.

```csharp
public async Task<ErrorOr<Unit>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
{
    var user = await userManager.FindByEmailAsync(command.Email);
    if (user is not null)
    {
        logger.LogWarning("The {Email} is already in use", command.Email);
        return Error.Conflict(ErrorCodes.Account.EmailExists);
    }

    var createResult = await userManager.CreateAsync(account, temporaryPassword);
    if (!createResult.Succeeded)
    {
        logger.LogError("Failed to create account for {Email}: {Errors}",
            command.Email,
            string.Join(", ", createResult.Errors.Select(e => e.Code)));
        return Error.Failure(ErrorCodes.Account.CreateFailed);
    }

    return Unit.Value; // Success
}
```

### Layer 2: Unexpected Exceptions — GlobalExceptionHandler

Catches all unhandled exceptions and converts them to ProblemDetails:

```csharp
internal sealed class GlobalExceptionHandler(
    IHostEnvironment env,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail, logLevel) = MapException(exception);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = env.IsDevelopment() ? exception.ToString() : detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path,
        };

        if (env.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().FullName;
            if (exception.InnerException != null)
                problem.Extensions["innerException"] = exception.InnerException.Message;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
```

### Exception → HTTP Status Mapping

| Exception | Status | Title |
|-----------|--------|-------|
| `DbUpdateConcurrencyException` | 409 | Concurrency Conflict |
| `DbUpdateException` (duplicate key) | 409 | Duplicate Resource |
| `DbUpdateException` (other) | 500 | Database Error |
| `ArgumentNullException` | 400 | Bad Request |
| `ArgumentException` | 400 | Invalid Argument |
| `KeyNotFoundException` | 404 | Not Found |
| `InvalidOperationException` | 400 | Invalid Operation |
| `UnauthorizedAccessException` | 403 | Forbidden |
| `TimeoutException` | 504 | Request Timeout |
| `OperationCanceledException` | 499 | Request Cancelled |
| Any other | 500 | Internal Server Error |

## ProblemDetails Customization

All ProblemDetails responses include additional fields via `ProblemDetailsExtensions.cs`:

```csharp
public static IServiceCollection AddCustomizedProblemDetails(this IServiceCollection services)
{
    services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = ctx =>
        {
            var http = ctx.HttpContext;

            // Trace ID for correlating with OpenTelemetry
            ctx.ProblemDetails.Extensions["traceId"] = http.TraceIdentifier;

            // Correlation ID from middleware (X-Correlation-Id header)
            if (http.Items.TryGetValue("CorrelationId", out var cid) && cid is string correlationId)
            {
                ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
            }

            // RFC 7807 Type URL
            ctx.ProblemDetails.Type ??= $"https://httpstatuses.com/{ctx.ProblemDetails.Status ?? 500}";
        };
    });
    return services;
}
```

Example response body:

```json
{
    "type": "https://httpstatuses.com/400",
    "title": "Validation Error",
    "status": 400,
    "detail": "First name is required.",
    "instance": "/api/v1/accounts",
    "traceId": "00-abc123...",
    "correlationId": "d4f5a6b7-...",
    "errors": ["Account.FirstName.Required", "Account.Email.Required"]
}
```

## ErrorOr → HTTP Result Mapping

Extension methods in `Acme.Api/Extensions/TypedResultsExtensions.cs`:

### ToTypedResult (GET, general use)

```csharp
public static Results<Ok<TValue>, ProblemHttpResult, NotFound, Conflict>
    ToTypedResult<TValue>(this ErrorOr<TValue> result)
{
    if (result.IsError)
    {
        var firstError = result.FirstError;
        return firstError.Type switch
        {
            ErrorType.Validation => CreateProblem(result.Errors, firstError),
            ErrorType.NotFound => TypedResults.NotFound(),
            ErrorType.Conflict => CreateProblem(result.Errors, firstError, StatusCodes.Status409Conflict),
            _ => CreateProblem(result.Errors, firstError)
        };
    }
    return TypedResults.Ok(result.Value);
}
```

### ToCreatedResult (POST)

```csharp
public static Results<Created<TValue>, ProblemHttpResult, Conflict>
    ToCreatedResult<TValue>(this ErrorOr<TValue> result, string uri)
{
    if (result.IsError)
    {
        var firstError = result.FirstError;
        return firstError.Type switch
        {
            ErrorType.Validation => CreateProblem(result.Errors, firstError),
            ErrorType.Conflict => CreateProblem(result.Errors, firstError, StatusCodes.Status409Conflict),
            _ => CreateProblem(result.Errors, firstError)
        };
    }
    return TypedResults.Created(uri, result.Value);
}
```

### ToNoContentResult (DELETE, PUT)

```csharp
public static Results<NoContent, ProblemHttpResult, NotFound>
    ToNoContentResult<TValue>(this ErrorOr<TValue> result)
{
    if (result.IsError)
    {
        var firstError = result.FirstError;
        return firstError.Type switch
        {
            ErrorType.Validation => CreateProblem(result.Errors, firstError),
            ErrorType.NotFound => TypedResults.NotFound(),
            _ => CreateProblem(result.Errors, firstError)
        };
    }
    return TypedResults.NoContent();
}
```

### ToAuthResult (Authentication endpoints)

```csharp
public static Results<Ok<TValue>, ProblemHttpResult, NotFound, UnauthorizedHttpResult>
    ToAuthResult<TValue>(this ErrorOr<TValue> result)
{
    if (result.IsError)
    {
        var firstError = result.FirstError;
        return firstError.Type switch
        {
            ErrorType.Validation => CreateProblem(result.Errors, firstError),
            ErrorType.NotFound => TypedResults.NotFound(),
            ErrorType.Unauthorized => TypedResults.Unauthorized(),
            _ => CreateProblem(result.Errors, firstError)
        };
    }
    return TypedResults.Ok(result.Value);
}
```

### Shared CreateProblem Helper

```csharp
private static ProblemHttpResult CreateProblem(
    List<Error> errors,
    Error firstError,
    int statusCode = StatusCodes.Status400BadRequest)
{
    var errorCodes = errors.Select(e => e.Code).Distinct().ToList();

    if (errors.Count > 1 && errors.All(e => e.Type == ErrorType.Validation))
    {
        return TypedResults.Problem(
            statusCode: statusCode,
            title: "Validation Error",
            detail: firstError.Description,
            extensions: new Dictionary<string, object?> { ["errors"] = errorCodes });
    }

    return TypedResults.Problem(
        statusCode: statusCode,
        title: firstError.Code,
        extensions: new Dictionary<string, object?> { ["errors"] = errorCodes });
}
```

Logic:
- Multiple validation errors → title = "Validation Error", detail = first error description
- Single/mixed errors → title = first error code
- `errors` extension always contains all distinct error codes

## Error Flow Summary

```
Request
  → ValidationBehavior (FluentValidation)
      → Failures? → ErrorOr<T> with validation errors → ToTypedResult() → 400 ProblemDetails
  → Handler
      → Business error? → ErrorOr<T> with Error.Conflict/NotFound/etc → ToTypedResult() → 4xx ProblemDetails
      → Success? → ErrorOr<T> with value → ToTypedResult() → 200/201/204
  → Unexpected exception?
      → GlobalExceptionHandler → ProblemDetails → 4xx/5xx
```

## Error Code Conventions

```csharp
// File: Acme.Domain/Constants/ErrorCodes.cs
public static class ErrorCodes
{
    public static class Account
    {
        // Required fields: {Domain}.{Property}.Required
        public const string FirstNameEmpty = "Account.FirstName.Required";
        public const string EmailEmpty = "Account.Email.Required";
        public const string PasswordEmpty = "Account.Password.Required";
        public const string RoleEmpty = "Account.Role.Required";

        // Invalid value: {Domain}.{Property}.Invalid
        public const string RoleInvalid = "Account.Role.Invalid";

        // Business rule: {Domain}.{Property}.{Rule}
        public const string DeveloperRoleNotAllowed = "Account.Role.DeveloperNotAllowed";

        // Operation failure: {Domain}.{Operation}
        public const string CreateFailed = "Account.CreateFailed";
        public const string EmailExists = "Account.EmailExists";
        public const string InsufficientPermissions = "Account.InsufficientPermissions";
    }

    // Add new feature error codes as nested static classes:
    // public static class Product { ... }
    // public static class Order { ... }
}
```
