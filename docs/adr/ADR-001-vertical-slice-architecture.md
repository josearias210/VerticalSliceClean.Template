# ADR-001: Vertical Slice Architecture

**Status:** Accepted  
**Date:** 2025-01-15  
**Deciders:** Jose Arias

---

## Context

Traditional layered architecture (N-tier) with horizontal layers (Controllers, Services, Repositories, Domain) leads to:
- **High coupling** between layers
- **Difficult feature isolation** (changes ripple across layers)
- **Code scattered** across multiple folders
- **Merge conflicts** when multiple developers work on different features

We needed an architecture that:
- Minimizes coupling between features
- Makes features easy to add/remove
- Reduces cognitive load (everything for a feature in one place)
- Improves team productivity (parallel development without conflicts)

---

## Decision

We will use **Vertical Slice Architecture** where each feature is self-contained:

### Structure:
```
Application/Features/
├── Products/
│   ├── CreateProduct/
│   │   ├── CreateProductCommand.cs          # Request
│   │   ├── CreateProductCommandHandler.cs   # Business logic
│   │   ├── CreateProductCommandValidator.cs # Validation
│   │   └── CreateProductCommandResponse.cs  # Response
│   └── GetProducts/
│       ├── GetProductsQuery.cs
│       ├── GetProductsQueryHandler.cs
│       └── GetProductsQueryResponse.cs
```

### Principles:
1. **One feature = One folder** (Command/Query + Handler + Validator + Response)
2. **No shared business logic** layers (Services, Repositories)
3. **Direct DbContext access** in handlers (no repository pattern)
4. **Feature-level cohesion** (everything related to the feature is together)
5. **Loose coupling** between features (features don't call each other)

### CQRS with MediatR:
- **Commands** for writes (CreateProduct, UpdateProduct)
- **Queries** for reads (GetProducts, GetProductById)
- **Handlers** contain all business logic
- **Validators** use FluentValidation

---

## Consequences

### Positive:
- ✅ **Easy to add features** (just create a new folder)
- ✅ **Easy to remove features** (delete the folder)
- ✅ **No merge conflicts** (features don't overlap)
- ✅ **Fast onboarding** (developers only need to understand one slice)
- ✅ **Clear boundaries** (feature scope is obvious)
- ✅ **Testable** (handlers are isolated and easy to test)

### Negative:
- ❌ **Code duplication** (shared logic must be duplicated or extracted to shared utilities)
- ❌ **Learning curve** (developers used to N-tier need to adapt)
- ❌ **Less guidance** (no enforced layering, developers must be disciplined)

### Mitigations:
- Use **shared utilities** for truly cross-cutting concerns (e.g., DateTimeProvider)
- Use **domain events** for cross-feature communication (if needed)
- Use **architectural tests** (ArchUnit) to enforce boundaries

---

## Alternatives Considered

### 1. Clean Architecture with N-tier layers
- **Pros:** Well-known, enforced boundaries
- **Cons:** High coupling, difficult to isolate features, slow development

### 2. Modular Monolith
- **Pros:** Strong boundaries between modules
- **Cons:** Overhead for small projects, module boundaries hard to define

### 3. Microservices
- **Pros:** Ultimate feature isolation
- **Cons:** Distributed complexity, operational overhead

---

## References

- [Vertical Slice Architecture by Jimmy Bogard](https://www.youtube.com/watch?v=SUiWfhAhgQw)
- [CQRS with MediatR](https://github.com/jbogard/MediatR)
- [Feature Slices for ASP.NET Core MVC](https://github.com/jbogard/MediatR/wiki)
