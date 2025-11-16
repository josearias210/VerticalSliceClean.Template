# ADR-003: Typed Results for HTTP Responses

**Status:** Accepted  
**Date:** 2025-01-15  
**Deciders:** Jose Arias

---

## Context

Traditional ASP.NET Core endpoints return `IResult` or `IActionResult`:
- **No compile-time type safety** (any status code can be returned)
- **Manual OpenAPI configuration** (`.Produces<T>(200)`, `.ProducesProblem(404)`, etc.)
- **Runtime errors** (wrong status code or response type only caught at runtime)
- **Verbose boilerplate** (repetitive metadata for each endpoint)

We needed an approach that:
- Provides **compile-time guarantees** for response types
- **Automatically generates OpenAPI** documentation
- **Reduces boilerplate** in endpoint definitions
- **Improves testability** (clear return type contracts)

---

## Decision

We will use **Typed Results** (ASP.NET Core 7+):

### Definition:
```csharp
// Endpoint explicitly declares ALL possible responses at compile-time
products.MapPost("/", async (ISender sender, CreateProductCommand cmd) =>
    (await sender.Send(cmd)).ToCreatedResult("/api/v1/products"))
.WithMetadata("Create Product", "Creates a new product with name and price");

// ToCreatedResult() returns:
// Results<Created<Product>, ValidationProblem, Conflict>
```

### Return Types:
```csharp
// Query endpoints (GET)
Results<Ok<TValue>, ValidationProblem, NotFound, Conflict>

// Creation endpoints (POST)
Results<Created<TValue>, ValidationProblem, Conflict>

// Update/Delete endpoints (PUT/DELETE)
Results<NoContent, ValidationProblem, NotFound>

// Authentication endpoints
Results<Ok<TValue>, ValidationProblem, NotFound, UnauthorizedHttpResult>
```

### Extension Methods:
```csharp
public static class TypedResultsExtensions
{
    public static Results<Ok<TValue>, ValidationProblem, NotFound, Conflict> ToTypedResult<TValue>(
        this ErrorOr<TValue> result)
    {
        if (result.IsError)
        {
            return result.FirstError.Type switch
            {
                ErrorType.Validation => CreateValidationProblem(result.Errors),
                ErrorType.NotFound => TypedResults.NotFound(),
                ErrorType.Conflict => TypedResults.Conflict(),
                _ => CreateValidationProblem(result.Errors)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    // Similar methods for ToCreatedResult, ToNoContentResult, ToAuthResult
}
```

---

## Consequences

### Positive:
- ✅ **Compile-time safety** (cannot return undeclared status codes)
- ✅ **Automatic OpenAPI** (no manual `.Produces<T>()` needed)
- ✅ **Reduced boilerplate** (~250 lines removed from codebase)
- ✅ **Better testability** (clear contracts for testing)
- ✅ **Refactoring confidence** (compiler catches breaking changes)
- ✅ **Self-documenting** (return type shows all possible outcomes)

### Negative:
- ❌ **Requires .NET 7+** (not available in older versions)
- ❌ **Verbose union types** (long `Results<...>` declarations)
- ❌ **Limited to declared types** (cannot return arbitrary status codes)

### Mitigations:
- Use **extension methods** (`ToTypedResult()`, `ToCreatedResult()`) to encapsulate complexity
- Group common return type patterns in helpers
- Leverage **type inference** where possible to reduce verbosity

---

## Alternatives Considered

### 1. IResult with manual OpenAPI configuration
```csharp
.Produces<Product>(200)
.ProducesProblem(400)
.ProducesProblem(404)
.ProducesProblem(409)
```
- **Pros:** Flexible, works in all .NET versions
- **Cons:** No compile-time safety, verbose, error-prone

### 2. ActionResult<T> (MVC Controllers)
- **Pros:** Some type safety for success case
- **Cons:** Still requires manual Produces attributes, tied to MVC

### 3. Custom Result<T> wrapper
- **Pros:** Full control over implementation
- **Cons:** Reinventing framework features, no built-in OpenAPI support

---

## Implementation Details

### Before (250+ lines of boilerplate):
```csharp
accounts.MapPost("/login", async (ISender sender, LoginCommand cmd) =>
{
    var result = await sender.Send(cmd);
    return result.ToHttpResponse(); // Runtime decision, no compile-time check
})
.WithAuthResponses<LoginResponse>() // Manual configuration
.WithMetadata(...);
```

### After (Typed Results):
```csharp
accounts.MapPost("/login", async (ISender sender, LoginCommand cmd) =>
    (await sender.Send(cmd)).ToAuthResult())
.WithMetadata("Login", "Authenticates user with email and password");
// OpenAPI automatically inferred from Results<Ok<LoginResponse>, ValidationProblem, NotFound, UnauthorizedHttpResult>
```

### Removed Extensions:
- `WithAuthResponses<T>()`
- `WithCreationResponses<T>()`
- `WithQueryResponses<T>()`
- `WithNoContentResponses()`
- `ToHttpResponse()` methods (~150 lines)

### OpenAPI Integration:
```csharp
public static RouteHandlerBuilder WithMetadata(
    this RouteHandlerBuilder builder,
    string summary,
    string description)
{
    return builder
        .WithOpenApi(operation =>
        {
            operation.Summary = summary;
            operation.Description = description;
            return operation;
        })
        .WithTags("v1");
}
```

---

## Migration Impact

**Code Reduction:**
- Removed: ~250 lines (manual OpenAPI configuration, ToHttpResponse methods)
- Added: ~125 lines (TypedResultsExtensions with 4 conversion methods)
- **Net reduction: ~125 lines** with better type safety

**All Endpoints Migrated:**
- Authentication: `login`, `refresh`, `logout`, `me`, `register` (5 endpoints)
- Products: `create`, `getAll` (2 endpoints)
- **Total: 7 endpoints** now using Typed Results

---

## References

- [ASP.NET Core Typed Results](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses)
- [OpenAPI Support for Typed Results](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi)
- [Typed Results Best Practices](https://andrewlock.net/behind-the-scenes-of-minimal-apis-8-customising-responses-with-iresult/)
