# ADR-002: ErrorOr Pattern for Error Handling

**Status:** Accepted  
**Date:** 2025-01-15  
**Deciders:** Jose Arias

---

## Context

Traditional error handling in .NET uses exceptions for control flow:
- **Performance overhead** (exception throwing is expensive)
- **Unclear API contracts** (what errors can a method return?)
- **Mixed concerns** (business errors vs system errors)
- **Implicit control flow** (exceptions break normal execution)

We needed an error handling approach that:
- Makes errors **explicit** and **type-safe**
- Avoids exceptions for **expected** business errors
- Provides clear **API contracts** (return types show all possible outcomes)
- Integrates well with **functional programming** principles

---

## Decision

We will use the **ErrorOr pattern** (functional error handling):

### Library:
[ErrorOr by Amichai Mantinband](https://github.com/amantinband/error-or)

### Usage:
```csharp
// Handler returns ErrorOr<T>
public async Task<ErrorOr<Product>> Handle(CreateProductCommand command, ...)
{
    // Business validation
    if (await _dbContext.Products.AnyAsync(p => p.Name == command.Name))
    {
        return Error.Conflict(
            code: "Product.DuplicateName",
            description: $"Product with name '{command.Name}' already exists."
        );
    }

    // Success path
    var product = new Product { Name = command.Name, Price = command.Price };
    _dbContext.Products.Add(product);
    await _dbContext.SaveChangesAsync();
    
    return product;
}
```

### Error Types:
- **Validation** → 400 Bad Request
- **NotFound** → 404 Not Found
- **Conflict** → 409 Conflict
- **Unauthorized** → 401 Unauthorized
- **Forbidden** → 403 Forbidden
- **Failure** → 500 Internal Server Error

### Conversion to HTTP:
```csharp
// Typed Results automatically map ErrorOr to HTTP responses
products.MapPost("/", async (ISender sender, CreateProductCommand cmd) =>
    (await sender.Send(cmd)).ToCreatedResult("/api/v1/products"))
.WithMetadata(...);
```

---

## Consequences

### Positive:
- ✅ **Explicit errors** (return type shows all possible outcomes)
- ✅ **Type-safe** (compiler enforces error handling)
- ✅ **Performance** (no exception throwing for business errors)
- ✅ **Functional composition** (can chain operations with `Match`, `Then`, etc.)
- ✅ **Clear separation** (business errors vs system errors)
- ✅ **Better testability** (errors are values, not exceptions)

### Negative:
- ❌ **Learning curve** (developers must learn functional patterns)
- ❌ **Verbosity** (more code than simple `throw` statements)
- ❌ **Less guidance** (developers must decide what's an error vs exception)

### Mitigations:
- Use **GlobalExceptionHandler** for **unexpected** exceptions (system errors)
- Reserve **exceptions** for truly **exceptional** situations (DB connection failure, etc.)
- Provide **TypedResultsExtensions** to simplify ErrorOr → HTTP mapping

---

## Alternatives Considered

### 1. Exceptions for control flow
- **Pros:** Simple, well-known
- **Cons:** Expensive, unclear API contracts, mixed concerns

### 2. Result<T, TError> (custom implementation)
- **Pros:** Similar benefits to ErrorOr
- **Cons:** Reinventing the wheel, less community support

### 3. OneOf (discriminated unions)
- **Pros:** More flexible return types
- **Cons:** Less ergonomic for error handling, no built-in error types

---

## Implementation Details

### FluentValidation Integration:
```csharp
// ValidationBehavior converts FluentValidation errors to Error.Validation()
var errors = failures.ConvertAll(f => Error.Validation(
    code: string.IsNullOrWhiteSpace(f.ErrorCode) ? f.PropertyName : f.ErrorCode,
    description: f.ErrorMessage
));

return (dynamic)errors;
```

### Typed Results Mapping:
```csharp
public static Results<Ok<TValue>, ValidationProblem, NotFound, Conflict> ToTypedResult<TValue>(
    this ErrorOr<TValue> result)
{
    if (result.IsError)
    {
        return result.FirstError.Type switch
        {
            ErrorType.Validation => TypedResults.ValidationProblem(...),
            ErrorType.NotFound => TypedResults.NotFound(),
            ErrorType.Conflict => TypedResults.Conflict(),
            _ => TypedResults.ValidationProblem(...)
        };
    }

    return TypedResults.Ok(result.Value);
}
```

---

## References

- [ErrorOr Library](https://github.com/amantinband/error-or)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Functional Error Handling in C#](https://www.youtube.com/watch?v=YR5WdGrpoug)
