# ADR-005: API Versioning Strategy

**Status:** Accepted  
**Date:** 2025-01-15  
**Deciders:** Jose Arias

---

## Context

APIs evolve over time, requiring breaking changes:
- **New required fields** (breaking existing clients)
- **Changed response shapes** (different DTOs)
- **Renamed endpoints** (different routes)
- **Removed features** (deprecated functionality)

Without versioning:
- **Breaking changes** force all clients to update simultaneously
- **No backward compatibility** for legacy clients
- **Coordination nightmare** (frontend and backend must deploy together)

We needed a versioning strategy that:
- **Supports multiple versions** simultaneously
- **Clear and predictable** version identification
- **Backward compatible** (old clients continue working)
- **Easy to implement** and understand

---

## Decision

We will use **URL path segment versioning**:

### Format:
```
/api/v{version}/{resource}

Examples:
/api/v1/accounts/login
/api/v1/products
/api/v2/products  (future)
```

### Implementation:
```csharp
public static class EndpointsExtensions
{
    public static void MapRoutes(this WebApplication app)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        // Group by version
        var v1 = app.MapGroup("/api/v1")
            .WithTags("v1")
            .WithOpenApi();

        // Register endpoints with version prefix
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(v1);
        }
    }
}
```

### Swagger Configuration:
```csharp
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Acme Acme API",
        Description = "RESTful API with JWT authentication"
    });
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Acme Acme API v1");
});
```

---

## Consequences

### Positive:
- ✅ **Explicit and visible** (version immediately clear in URL)
- ✅ **Easy to cache** (different URLs = different cache keys)
- ✅ **Simple routing** (no header parsing required)
- ✅ **Works everywhere** (browsers, curl, Postman)
- ✅ **Clear deprecation** (can remove old versions by deleting folder)
- ✅ **OpenAPI support** (each version has separate Swagger doc)

### Negative:
- ❌ **URL pollution** (version in every endpoint)
- ❌ **Routing duplication** (must map each version separately)
- ❌ **Resource proliferation** (multiple versions of same endpoint)

### Mitigations:
- Use **MapGroup()** to reduce duplication (single version prefix for all endpoints)
- **Deprecate old versions** with sunset headers and documentation
- **Share common logic** between versions (only DTO mapping differs)

---

## Versioning Strategy

### When to Bump Version:
- **Breaking changes** (required):
  - Removing properties from response
  - Renaming fields
  - Changing data types
  - Removing endpoints
  - Changing authentication schemes

- **Non-breaking changes** (no version bump):
  - Adding new optional fields
  - Adding new endpoints
  - Fixing bugs
  - Performance improvements

### Version Lifecycle:
1. **Active** (v1) - Current production version
2. **Deprecated** (v0) - Marked with `Sunset` header, still functional
3. **Retired** (removed) - No longer available

### Example Sunset Header:
```csharp
app.MapGroup("/api/v0")
    .AddEndpointFilter(async (context, next) =>
    {
        context.HttpContext.Response.Headers["Sunset"] = "Sat, 31 Dec 2025 23:59:59 GMT";
        context.HttpContext.Response.Headers["Deprecation"] = "true";
        return await next(context);
    });
```

---

## Alternatives Considered

### 1. Header-based versioning
```
GET /api/accounts
Accept: application/vnd.acme.v1+json
```
- **Pros:** Clean URLs, RESTful purists prefer it
- **Cons:** Less discoverable, harder to test, requires header parsing

### 2. Query string versioning
```
GET /api/accounts?version=1
```
- **Pros:** Simple, works in browsers
- **Cons:** Breaks caching, easy to omit, pollutes query string

### 3. Subdomain versioning
```
GET https://v1.api.acme.com/accounts
```
- **Pros:** Clear separation, can deploy versions independently
- **Cons:** Requires multiple subdomains, SSL certificates, DNS setup

### 4. Media type versioning
```
GET /api/accounts
Accept: application/json;version=1
```
- **Pros:** RESTful, content negotiation
- **Cons:** Complex, poor tooling support

---

## Implementation Details

### Current Structure:
```
src/Acme.Api/Endpoints/
├── AccountsEndpoints.cs   (v1 endpoints)
└── ProductsEndpoints.cs   (v1 endpoints)
```

### Future Structure (when v2 needed):
```
src/Acme.Api/Endpoints/
├── V1/
│   ├── AccountsEndpoints.cs
│   └── ProductsEndpoints.cs
├── V2/
│   ├── AccountsEndpoints.cs  (breaking changes)
│   └── ProductsEndpoints.cs  (breaking changes)
```

### Endpoint Registration (Future):
```csharp
public static void MapRoutes(this WebApplication app)
{
    var v1Endpoints = app.Services.GetKeyedService<IEnumerable<IEndpoint>>("v1");
    var v2Endpoints = app.Services.GetKeyedService<IEnumerable<IEndpoint>>("v2");

    var v1Group = app.MapGroup("/api/v1").WithTags("v1");
    var v2Group = app.MapGroup("/api/v2").WithTags("v2");

    foreach (var endpoint in v1Endpoints)
        endpoint.MapEndpoint(v1Group);

    foreach (var endpoint in v2Endpoints)
        endpoint.MapEndpoint(v2Group);
}
```

---

## Migration Path

### Adding v2 (Example):
```csharp
// V1 - Original
public record CreateProductCommand(string Name, decimal Price);
public record ProductResponse(int Id, string Name, decimal Price);

// V2 - Breaking change (required Category field)
public record CreateProductCommandV2(string Name, decimal Price, string Category);
public record ProductResponseV2(int Id, string Name, decimal Price, string Category, DateTime CreatedAt);
```

### Shared Logic:
```csharp
// Application layer handles both versions
public class CreateProductCommandHandler :
    IRequestHandler<CreateProductCommand, ErrorOr<Product>>,
    IRequestHandler<CreateProductCommandV2, ErrorOr<Product>>
{
    // Both versions use same domain logic
    public async Task<ErrorOr<Product>> Handle(CreateProductCommand request, ...)
    {
        // Default category for v1
        return await CreateProductInternal(request.Name, request.Price, "Uncategorized");
    }

    public async Task<ErrorOr<Product>> Handle(CreateProductCommandV2 request, ...)
    {
        return await CreateProductInternal(request.Name, request.Price, request.Category);
    }
}
```

---

## References

- [Microsoft API Versioning Guidance](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design#versioning-a-restful-web-api)
- [Semantic Versioning](https://semver.org/)
- [RFC 8594 - Sunset HTTP Header](https://datatracker.ietf.org/doc/html/rfc8594)
- [Troy Hunt - Your API versioning is wrong](https://www.troyhunt.com/your-api-versioning-is-wrong-which-is/)
