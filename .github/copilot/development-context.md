# Development Context for GitHub Copilot

> Purpose: Guide Copilot to generate code aligned with our conventions.

## Defaults
- C#: target `net10.0`, use C# 14 features
- Use primary constructors and file-scoped namespaces
- Use records for DTOs and responses
- Use MediatR + ErrorOr patterns
- Validators with FluentValidation
- Handlers return `ErrorOr<T>`

## Naming & Structure
- Features in `Application/Features/{Entity}/{Operation}`
- Endpoints in `Api/Endpoints/{Entity}Endpoints.cs`
- Configurations in `Infrastructure/Persistence/EF/Configurations`

## Endpoint Patterns
- Grouped with `MapV1Group("resource")`
- Use `.RequireAuthorization()` for protected endpoints
- Return Typed Results via extension methods

## EF Core
- Inject `IApplicationDbContext`
- Use `AsNoTracking()` for reads
- Use projections rather than materializing entities when possible
- Add indexes for common filters and sorting

## Error Handling
- Use centralized `ErrorCodes`
- No magic strings for error codes
- Prefer small focused handlers
