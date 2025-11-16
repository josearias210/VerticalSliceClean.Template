# ADR-004: JWT Authentication with httpOnly Cookies

**Status:** Accepted  
**Date:** 2025-01-15  
**Deciders:** Jose Arias

---

## Context

Traditional JWT authentication stores tokens in **localStorage** or **sessionStorage**:
- **Vulnerable to XSS** (JavaScript can access tokens)
- **Manual token management** (client must handle refresh logic)
- **No automatic expiration** (tokens remain until manually removed)
- **CSRF not a concern** (Authorization header not sent with forms)

We needed a secure authentication approach that:
- **Protects against XSS** (JavaScript cannot access tokens)
- **Simplifies client code** (browser handles token storage/sending)
- **Provides automatic refresh** (seamless UX)
- **Mitigates CSRF** (requires SameSite cookies + additional protection)

---

## Decision

We will use **JWT tokens stored in httpOnly cookies**:

### Token Strategy:
- **Access Token** (short-lived, 15 minutes)
  - Stored in httpOnly cookie: `.AspNetCore.AccessToken`
  - Used for API authorization
  - Cannot be accessed by JavaScript
  
- **Refresh Token** (long-lived, 7 days)
  - Stored in httpOnly cookie: `.AspNetCore.RefreshToken`
  - Used to obtain new access tokens
  - One-time use (revoked after refresh)
  - Stored in database with expiration tracking

### Cookie Configuration:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Read access token from cookie
                context.Token = context.Request.Cookies[".AspNetCore.AccessToken"];
                return Task.CompletedTask;
            }
        };
    });
```

### Login Flow:
```csharp
public async Task<ErrorOr<LoginResponse>> Handle(LoginCommand request, ...)
{
    // Validate credentials...
    
    var accessToken = _tokenService.GenerateAccessToken(account);
    var refreshToken = _tokenService.GenerateRefreshToken();
    
    // Store refresh token in database
    account.RefreshTokens.Add(new RefreshToken
    {
        Token = refreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow
    });
    
    // Set httpOnly cookies
    _httpContextAccessor.HttpContext.Response.Cookies.Append(
        ".AspNetCore.AccessToken",
        accessToken,
        new CookieOptions
        {
            HttpOnly = true,        // JavaScript cannot access
            Secure = true,          // HTTPS only
            SameSite = SameSiteMode.Strict, // CSRF protection
            MaxAge = TimeSpan.FromMinutes(15)
        });
    
    _httpContextAccessor.HttpContext.Response.Cookies.Append(
        ".AspNetCore.RefreshToken",
        refreshToken,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(7)
        });
    
    return new LoginResponse { Message = "Login successful" };
}
```

---

## Consequences

### Positive:
- ✅ **XSS protection** (httpOnly cookies inaccessible to JavaScript)
- ✅ **Simplified client** (browser handles cookies automatically)
- ✅ **CSRF mitigation** (SameSite=Strict blocks cross-site requests)
- ✅ **Automatic expiration** (MaxAge enforces client-side cleanup)
- ✅ **Token rotation** (refresh tokens are one-time use)
- ✅ **Reuse detection** (revokes all tokens if refresh token reused)

### Negative:
- ❌ **CORS complexity** (requires `credentials: 'include'` in fetch requests)
- ❌ **Mobile apps** (cookies less ergonomic than Bearer tokens)
- ❌ **CSRF risk** (mitigated with SameSite=Strict + CORS restrictions)

### Mitigations:
- **CORS:** Configure `AllowCredentials()` with explicit origins (no wildcard)
- **CSRF:** Use `SameSite=Strict` + validate Origin/Referer headers
- **Mobile:** Consider supporting Bearer tokens for native apps (future enhancement)

---

## Security Features

### 1. Account Lockout
```csharp
// 5 failed attempts in 30 minutes locks account
if (account.FailedLoginAttempts >= 5)
{
    account.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
}
```

### 2. Token Reuse Detection
```csharp
// If refresh token already used, revoke ALL tokens (indicates potential breach)
if (refreshToken.IsRevoked || refreshToken.IsUsed)
{
    // Security breach detected - revoke all tokens
    foreach (var token in account.RefreshTokens)
    {
        token.RevokedAt = DateTime.UtcNow;
    }
}
```

### 3. Automatic Cleanup
```csharp
// Background job runs daily at 3 AM to remove expired tokens
public class TokenCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredTokensAsync();
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

### 4. One-Time Refresh Tokens
```csharp
// Mark refresh token as used immediately after validation
refreshToken.IsUsed = true;
refreshToken.ReplacedByToken = newRefreshToken.Token;
```

---

## Alternatives Considered

### 1. JWT in localStorage
- **Pros:** Simple, works everywhere (web/mobile)
- **Cons:** Vulnerable to XSS attacks

### 2. JWT in Authorization header
- **Pros:** Standard, works with all clients, no CSRF risk
- **Cons:** Requires manual token storage, client-side refresh logic

### 3. Session-based authentication
- **Pros:** Server-side revocation, no token expiration issues
- **Cons:** Stateful (requires server-side storage), scaling challenges

### 4. Refresh token rotation (current choice)
- **Pros:** Balance of security and UX, token reuse detection
- **Cons:** More complex than simple JWT, requires database storage

---

## Implementation Details

### CORS Configuration:
```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:5173") // Explicit origin
              .AllowCredentials()  // Required for cookies
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### Client-Side (Frontend):
```javascript
// Fetch with credentials to send cookies
fetch('https://localhost:7001/api/v1/accounts/me', {
    method: 'GET',
    credentials: 'include' // Send cookies with request
})
```

### Refresh Endpoint:
```csharp
accounts.MapPost("/refresh", async (ISender sender, HttpContext context) =>
{
    var refreshToken = context.Request.Cookies[".AspNetCore.RefreshToken"];
    var result = await sender.Send(new RefreshTokenCommand { RefreshToken = refreshToken });
    return result.ToAuthResult();
})
.WithMetadata("Refresh Token", "Obtains new access token using refresh token")
.AllowAnonymous();
```

---

## References

- [OWASP JWT Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [Token Storage Best Practices](https://auth0.com/docs/secure/security-guidance/data-security/token-storage)
- [SameSite Cookie Specification](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie/SameSite)
- [Refresh Token Rotation](https://auth0.com/docs/secure/tokens/refresh-tokens/refresh-token-rotation)
