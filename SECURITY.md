# Security Overview

This document describes the security model, controls, and operational safeguards implemented in the Acme Acme API.

## 1. Authentication & Authorization
- JWT-based authentication using Access + Refresh tokens.
- Access Token lifetime: 15 minutes (short-lived, reduces replay window).
- Refresh Token lifetime: 7 days (rotated on each refresh, stored server-side).
- Tokens delivered and stored in httpOnly, Secure, SameSite=Strict cookies to mitigate XSS & CSRF.
- Authorization enforced via `[Authorize]` attributes and middleware (role claims from JWT).

### Login Flow
1. User submits email + password to `/api/v1/accounts/login` (anonymous).
2. Credentials validated; account lockout checked.
3. Access Token + Refresh Token issued and set as httpOnly cookies.
4. Client automatically sends Access Token cookie with subsequent requests.

### Refresh Flow
1. Client calls `/api/v1/accounts/refresh` (anonymous) with existing Refresh Token cookie.
2. Refresh Token validated (not expired, not revoked, not reused).
3. Refresh Token marked used and replaced; new Access + Refresh tokens issued.
4. If a used or revoked token is reused → all account tokens revoked (breach containment).

### Logout Flow
1. Client calls `/api/v1/accounts/logout`.
2. All refresh tokens for the account revoked (multi-device logout) or a specific token if scoped (future enhancement).

## 2. Token Lifecycle & Rotation
- Each refresh action invalidates (marks IsUsed + RevokedAt) the old token.
- Token reuse detection triggers a full revocation sweep for that user.
- Expired or revoked tokens are physically deleted daily (3 AM) by `TokenCleanupService`.

### Refresh Token States
| State        | Meaning                                      |
|--------------|-----------------------------------------------|
| Active       | Usable for refresh                            |
| Used         | Successfully exchanged for a new token        |
| Revoked      | Manually invalidated (logout/security event)  |
| Expired      | Past `ExpiresAt` timestamp                    |

## 3. Account Lockout Policy
- Tracks failed login attempts in a 30-minute window.
- 5 failed attempts → account locked for 30 minutes.
- Successful login resets counters.
- Prevents brute force attacks against credentials.

## 4. Password Handling
- Passwords stored using a strong hashing algorithm (e.g., PBKDF2 via ASP.NET Identity or similar – confirm implementation in infrastructure layer).
- Minimum complexity enforced on creation: length ≥ 12–16 chars, mix of cases, digits, symbols (enforced during registration/administration).
- No plaintext persistence; passwords never logged.
- Admin bootstrap password requires immediate rotation after first login (documented in README).

## 5. Token Reuse Detection (Replay Protection)
- Each Refresh Token is single-use.
- If a Refresh Token already marked as `IsUsed` or `Revoked` is presented again → potential token theft detected.
- Response behavior:
  - All existing refresh tokens for the account are revoked.
  - (Future) Security alert event can be published.
  - (Future) Email notification to user.

## 6. Background Cleanup
- `TokenCleanupService` runs daily at 03:00 server local time.
- Deletes tokens where `RevokedAt != null` OR `ExpiresAt < UtcNow`.
- Reduces database footprint and limits exposure of stale credentials.

## 7. Rate Limiting (Planned / Placeholder)
- Sliding window or fixed window strategy recommended for:
  - Login endpoint
  - Refresh endpoint
  - High-read endpoints (Products list)
- Suggested baseline:
  - `/login`: 10 requests / 5 minutes / IP
  - `/refresh`: 30 requests / hour / account
- Implementation options:
  - ASP.NET built-in rate limiting middleware (.NET 8+)
  - Reverse proxy layer (NGINX, Azure APIM, Cloudflare)

## 8. CORS Configuration
- Only explicit origins allowed (no `*`).
- `AllowCredentials()` enabled to support httpOnly cookies.
- Recommended prod config:
  - Allow methods: `GET, POST, PUT, DELETE, OPTIONS`.
  - Allow headers: `Content-Type, Authorization, X-Correlation-Id`.
  - Deny unknown origins by default.

### Example Policy
```csharp
services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("https://app.acme.com")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

## 9. CSRF Mitigation
- Uses SameSite=Strict cookies → browser will not send cookies on cross-site POST forms.
- All state-changing endpoints require JWT Access Token cookie.
- Optional enhancements (future):
  - Double-submit CSRF token for highly sensitive endpoints.
  - Validate `Origin` and `Referer` headers when present.

## 10. Security Headers
Configured via middleware (see `SecurityHeadersExtensions.cs`). Typical recommended set:
| Header                    | Purpose                                      |
|---------------------------|----------------------------------------------|
| `Strict-Transport-Security` | Enforce HTTPS usage                        |
| `X-Content-Type-Options`  | Prevent MIME sniffing                        |
| `X-Frame-Options`         | Prevent clickjacking (deny framing)          |
| `X-XSS-Protection`        | (Legacy) Basic XSS protection hints          |
| `Content-Security-Policy` | Restrict sources of scripts/styles/media      |
| `Referrer-Policy`         | Limit referrer leakage                       |
| `Permissions-Policy`      | Restrict powerful browser APIs               |

### Example CSP (adjust as needed)
```
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none';
```

## 11. Logging & Observability
- Serilog + OpenTelemetry used.
- Sensitive values (passwords, tokens) omitted from logs.
- Correlation ID middleware adds `X-Correlation-Id` for tracing multi-service flows.
- Authentication events (login success/failure, token refresh, reuse detection) recommended for structured logging.

## 12. Threat Mitigations Summary
| Threat                     | Mitigation                                      |
|----------------------------|-------------------------------------------------|
| Brute force login          | Account lockout + rate limiting                 |
| Token theft (XSS)          | httpOnly + Secure cookies                      |
| Token replay               | Single-use refresh + reuse detection            |
| Stale credentials          | Daily cleanup + short access token TTL          |
| CSRF                       | SameSite=Strict + origin restrictions           |
| MITM                       | HTTPS + HSTS                                    |
| SQL injection              | EF Core parameterization                        |
| Mass assignment            | Explicit DTO mapping                           |
| Leakage via logs           | Structured logging, no secrets in logs          |

## 13. Recommended Future Enhancements
- Add audit trail table (login, logout, password change events).
- Add email notification for suspicious token reuse.
- Implement rate limiting middleware.
- Add MFA / TOTP for high-privilege accounts.
- Implement anomaly detection (excessive refresh requests).
- Encrypt refresh tokens at rest (if risk model requires).

## 14. Operational Procedures
| Procedure            | Action                                                          |
|----------------------|------------------------------------------------------------------|
| Password Reset       | Generate secure token + email link (future feature).            |
| Compromised Account  | Revoke all tokens + force password change.                      |
| Token Reuse Alert    | Lock account temporarily + notify user (future enhancement).    |
| Data Export (GDPR)   | Provide user profile + related entities (future enhancement).   |

## 15. Validation & Testing (Planned)
- Integration tests for login / refresh / logout flows.
- Test token reuse scenario → expect total revocation.
- Load test authentication endpoints under concurrency.
- Security scan (OWASP ZAP) against staging environment.

## 16. References
- OWASP Cheat Sheets (Authentication, Session Management, JWT)
- RFC 7519 (JSON Web Tokens)
- RFC 6749 (OAuth 2.0 concepts for rotation inspiration)
- Auth0 / Okta Blogs on Refresh Token Rotation

---
**Last Reviewed:** 2025-01-15  
**Next Review Due:** 2025-04-15
