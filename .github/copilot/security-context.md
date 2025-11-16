# Security Context for GitHub Copilot

> Purpose: Provide consistent guidance for security-sensitive code generation.

## Authentication & Authorization
- Use JWT with HttpOnly cookies (no access token in JS)
- Require `.RequireAuthorization()` at endpoint group level
- Never check auth manually in handlers; use policies
- Use `IUserIdentityService.GetUserId()!` after policies

## Token Safety
- Store refresh tokens hashed in database
- Implement reuse detection (revoke all tokens if reused)
- Rotate refresh tokens on every refresh
- Revoke current token before issuing a new one

## Account Security
- Enforce account lockout on repeated failed logins
- Validate strong admin passwords on seeding (min 12 chars, mixed types)
- Log critical security events (login, refresh, revoke, token reuse)

## ProblemDetails & Errors
- Use `ErrorOr` for predictable errors
- Map to TypedResults via extension methods
- Centralize error codes in `ErrorCodes.cs`

## Data Handling
- Use `.AsNoTracking()` for queries
- Always filter by `CreatedByAccountId` for user-owned data
- Index composite fields for common filters

## Logging
- Use structured logging with contextual properties
- Security auditing should never break flows (wrap in try/catch)
