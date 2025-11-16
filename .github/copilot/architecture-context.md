# Architecture Context for GitHub Copilot

> **Purpose**: This file provides architectural context for GitHub Copilot to maintain consistency across code generation and refactoring tasks.

## Architecture Overview

**Pattern**: Vertical Slice Architecture with Clean Architecture principles
- Each feature is self-contained with its own Command/Query + Handler + Validator
- Features are organized by use case, not by technical layer
- CQRS pattern with MediatR for request/response flow
- ErrorOr pattern for explicit error handling without exceptions

## Project Structure

```
src/
├── Acme.Domain/          # Domain entities and enums (anemic for now)
├── Acme.Application/     # Use cases (MediatR handlers, validators, abstractions)
├── Acme.Infrastructure/  # Technical implementations (EF Core, JWT, Identity)
├── Acme.Api/            # Presentation layer (Minimal API endpoints)
└── Acme.AppHost/        # Composition root (startup, pipeline configuration)
```

## Layer Responsibilities

### Domain Layer
- **Contains**: Entities, Value Objects, Enums, Domain Events (future)
- **Current state**: Simple entities (anemic model is acceptable for now)
- **Dependencies**: None (pure POCO)
- **Example**: `Account`, `RefreshToken`, `TodoItem`, `Role` enum

### Application Layer
- **Contains**: Features (Commands/Queries), Handlers, Validators, Response DTOs, Abstractions
- **Pattern**: Vertical slices organized by feature (`Features/{FeatureName}/{Operation}/`)
- **Dependencies**: Domain, ErrorOr, MediatR, FluentValidation
- **No references to**: Infrastructure implementations (only interfaces)
- **Example**: `CreateTodoItem/CreateTodoItemCommand.cs`, `CreateTodoItemCommandHandler.cs`

### Infrastructure Layer
- **Contains**: EF Core DbContext, Identity configuration, JWT services, Email services, Migrations
- **Dependencies**: Application (abstractions), Domain, EF Core, ASP.NET Core Identity
- **Implements**: All application abstractions (`IApplicationDbContext`, `ITokenService`, etc.)

### API Layer (Presentation)
- **Contains**: Minimal API endpoints, Global exception handler, ProblemDetails extensions
- **Pattern**: Endpoint groups by resource (`/api/v1/todos`, `/api/v1/accounts`)
- **Dependencies**: Application (MediatR), TypedResults extensions
- **Returns**: Typed Results (`Results<Ok<T>, ValidationProblem, NotFound>`)

### AppHost Layer
- **Contains**: Program.cs, Pipeline configuration, Service registration orchestration
- **Purpose**: Composition root - wires up all layers
- **No business logic**: Only configuration and startup

## Key Patterns

### 1. Vertical Slice Feature Structure
```csharp
Application/Features/TodoItems/CreateTodoItem/
├── CreateTodoItemCommand.cs          // IRequest<ErrorOr<CreateTodoItemResponse>>
├── CreateTodoItemCommandValidator.cs // FluentValidation
├── CreateTodoItemCommandHandler.cs   // IRequestHandler
└── CreateTodoItemResponse.cs         // DTO
```

### 2. CQRS with MediatR
- **Commands**: Modify state, return `ErrorOr<TResponse>`
- **Queries**: Read data, return `ErrorOr<TResponse>`
- **Handlers**: One handler per command/query
- **Pipeline**: ValidationBehavior runs FluentValidation before handler

### 3. ErrorOr Pattern
```csharp
public async Task<ErrorOr<TodoItemResponse>> Handle(...)
{
    if (todoItem is null)
        return Error.NotFound(code: "TodoItem.NotFound", description: "...");
    
    return new TodoItemResponse(...); // Implicit conversion
}
```

### 4. Minimal API Endpoints
```csharp
public class TodoItemsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var todos = app.MapV1Group("todos").RequireAuthorization();
        
        todos.MapPost("", async (ISender sender, CreateTodoItemCommand cmd, CancellationToken ct) =>
            (await sender.Send(cmd, ct)).ToCreatedResult("/api/v1/todos"));
    }
}
```

## Data Access Strategy

### Direct DbContext Access (Preferred)
- Handlers inject `IApplicationDbContext` for full EF Core power
- No repository pattern for simple CRUD (YAGNI principle)
- Use `.AsNoTracking()` for read-only queries
- Use `.Include()`, projections, and LINQ directly

```csharp
public class GetTodoItemsQueryHandler(IApplicationDbContext dbContext) 
{
    var items = await dbContext.TodoItems
        .AsNoTracking()
        .Where(t => t.CreatedByAccountId == userId)
        .Select(t => new TodoItemResponse(...))
        .ToListAsync(ct);
}
```

### UserManager for Identity Operations
- For user/role/password operations, inject `UserManager<Account>` directly
- Use `SignInManager<Account>` for authentication logic
- Don't wrap Identity - use it directly in handlers

## Validation Strategy

### FluentValidation
- One validator per Command
- Validators run automatically via `ValidationBehavior` in MediatR pipeline
- Return `ErrorOr<T>` with validation errors
- Use `ErrorCode` property for custom error codes (e.g., "EmailAlreadyExists")

```csharp
public class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
    }
}
```

## Security Patterns

### Authorization
- Use `.RequireAuthorization()` on endpoint groups
- No manual `userId is null` checks in handlers (handled by policy)
- Use `IUserIdentityService.GetUserId()!` (null-forgiving operator safe here)

### Ownership Validation
- Use helper methods for repeated ownership checks
- Example: `TodoItemHelpers.ValidateOwnership(item, userId)`
- Return `Error.Forbidden()` for unauthorized access

### Error Codes
- Centralized in `ErrorCodes.cs` static class
- Format: `{Entity}.{ErrorType}` (e.g., `ErrorCodes.TodoItem.NotFound`)
- No magic strings in handlers

## Dependency Injection

### Registration Pattern
- Each layer has its own `DependencyInjection.cs` with extension method:
  - `AddDomain()` (if needed)
  - `AddApplication()` (MediatR, FluentValidation, behaviors)
  - `AddInfrastructure(config)` (DbContext, Identity, JWT, services)
  - `AddPresentation()` (Endpoints, ProblemDetails)
  - `AddHost(config)` (CORS, health checks, middleware)

### Service Lifetimes
- **Scoped**: DbContext, handlers, validators, services that hold state per request
- **Singleton**: Configuration, background services (TokenCleanupService)
- **Transient**: Rarely used (stateless utilities)

## Technology Stack

- **.NET 10** with C# 14
- **MediatR** for CQRS
- **FluentValidation** for input validation
- **ErrorOr** for functional error handling
- **EF Core** with SQL Server
- **ASP.NET Core Identity** for user management
- **JWT** with HttpOnly cookies for authentication
- **Serilog** for structured logging
- **OpenTelemetry** for observability

## Code Style Conventions

- Use **primary constructors** for dependency injection
- Use **file-scoped namespaces**
- Use **required** keyword for required properties
- Use **record** types for DTOs and responses
- Use **CancellationToken** parameter named `ct` or `cancellationToken`
- Use **XML documentation** on public APIs and entities
- Keep handlers **focused and small** (single responsibility)

## When Adding New Features

1. **Create vertical slice** in `Application/Features/{EntityName}/{Operation}/`
2. **Add Command/Query** record with `IRequest<ErrorOr<TResponse>>`
3. **Add Validator** if command modifies state
4. **Add Handler** implementing `IRequestHandler<TRequest, TResponse>`
5. **Add Response DTO** as record
6. **Create Endpoint** in `Api/Endpoints/{EntityName}Endpoints.cs`
7. **Update DbContext** if new entity (add DbSet and configuration)
8. **Create EF Configuration** in `Infrastructure/Persistence/EF/Configurations/`

## Testing Strategy (Future)

- Unit tests for handlers (mock `IApplicationDbContext`)
- Integration tests for endpoints (WebApplicationFactory)
- No repository mocks - test against in-memory or testcontainers DB
- Validator tests separate from handler tests

## Performance Considerations

- Use `.AsNoTracking()` for read-only queries
- Use projections (`.Select()`) instead of loading full entities
- Use composite indexes for common queries (CreatedByUserId + IsCompleted)
- Use pagination for list endpoints (add `PagedResult<T>` pattern when needed)

## YAGNI Principles Applied

- No repository pattern (DbContext is enough)
- No specification pattern (LINQ is enough)
- No mediator for events (MediatR commands/queries only)
- No domain services yet (handlers are sufficient)
- No complex domain models (anemic is fine for CRUD)

Add complexity only when there's real need, not "just in case".
