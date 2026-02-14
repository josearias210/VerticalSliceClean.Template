# Authentication & Authorization Reference

## OpenIddict OAuth2 Server

**File:** `src/Acme.Infrastructure/Extensions/IdentityExtensions.cs`

### Endpoints

| Endpoint | Path | Purpose |
|----------|------|---------|
| Token | `/connect/token` | Issue access/refresh tokens |
| Revocation | `/connect/revoke` | Revoke tokens |

### Allowed Flows

- **Password** (`AllowPasswordFlow`): Username + password → tokens
- **Refresh Token** (`AllowRefreshTokenFlow`): Refresh token → new tokens

### Scopes

`OpenId`, `Profile`, `OfflineAccess`, `Email`, `"api"` (custom)

### Token Lifetimes

| Token | Lifetime |
|-------|----------|
| Access Token | 15 minutes |
| Refresh Token | 14 days |
| Authorization Code | 5 minutes |

### Certificate Configuration

```csharp
if (environment.IsDevelopment())
{
    // Ephemeral certificates — auto-generated, change on restart
    options.AddDevelopmentEncryptionCertificate()
           .AddDevelopmentSigningCertificate();
    options.DisableAccessTokenEncryption();
}
else
{
    // Production: Load from file paths in configuration
    var encryptionCert = X509CertificateLoader.LoadPkcs12FromFile(
        openIddictSettings.EncryptionCertificatePath,
        openIddictSettings.CertificatePassword);
    var signingCert = X509CertificateLoader.LoadPkcs12FromFile(
        openIddictSettings.SigningCertificatePath,
        openIddictSettings.CertificatePassword);

    options.AddEncryptionCertificate(encryptionCert)
           .AddSigningCertificate(signingCert);
}
```

Throws `InvalidOperationException` if certificate paths are missing in production.

### OpenIddictSettings (`OpenIddict` section)

```json
{
    "OpenIddict": {
        "EncryptionCertificatePath": null,
        "SigningCertificatePath": null,
        "CertificatePassword": null
    }
}
```

### Authentication Scheme

```csharp
options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
```

---

## Password Grant Handler

**File:** `src/Acme.Infrastructure/Auth/OpenIddict/PasswordGrantHandler.cs`

Custom handler for the password flow:

```csharp
public class PasswordGrantHandler(
    UserManager<Account> userManager,
    SignInManager<Account> signInManager) : IOpenIddictServerHandler<HandleTokenRequestContext>
{
    public async ValueTask HandleAsync(HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType()) return;

        // 1. Find user by username OR email
        var user = await userManager.FindByNameAsync(context.Request.Username!)
                ?? await userManager.FindByEmailAsync(context.Request.Username!);

        if (user == null)
        {
            context.Reject(error: Errors.InvalidGrant, description: "...");
            return;
        }

        // 2. Validate password with lockout
        var result = await signInManager.CheckPasswordSignInAsync(user, context.Request.Password!, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            context.Reject(error: Errors.InvalidGrant, description: "...");
            return;
        }

        // 3. Create principal and set scopes
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        principal.SetScopes(context.Request.GetScopes());

        // 4. Ensure "sub" claim exists
        if (!principal.HasClaim(c => c.Type == Claims.Subject))
        {
            identity.AddClaim(new Claim(Claims.Subject, userId));
        }

        // 5. Set claim destinations (which token gets which claim)
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        context.SignIn(principal);
    }
}
```

### Claim Destinations

| Claim | Access Token | Identity Token (if scope) |
|-------|-------------|---------------------------|
| `name` | Always | If `profile` scope |
| `email` | Always | If `email` scope |
| `role` | Always | If `roles` scope |
| `sub` | Always | Always |
| `SecurityStamp` | Never | Never |
| Other | Always | Never |

---

## ASP.NET Core Identity

**File:** `src/Acme.Infrastructure/Extensions/IdentityExtensions.cs`

### Password Policies

```csharp
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredLength = 8;
```

### Account Lockout

```csharp
options.Lockout.AllowedForNewUsers = true;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
options.Lockout.MaxFailedAccessAttempts = 5;
```

### Identity Setup

```csharp
services.AddIdentityCore<Account>(options => { ... })
    .AddSignInManager()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

---

## Authorization Policies

**File:** `src/Acme.Infrastructure/Extensions/AuthorizationPoliciesExtensions.cs`

### Role-Based Policies

| Policy | Required Roles |
|--------|---------------|
| `"AdminOnly"` | Admin **or** Developer |
| `"CanManageProducts"` | Admin **or** ProductManager |
| `"CanManageUsers"` | Admin |

### Claim-Based Policies

| Policy | Required Claim |
|--------|---------------|
| `"EmailVerified"` | `email_verified = "true"` |
| `"MfaEnabled"` | `amr = "mfa"` |

### Scope-Based Policies (OAuth2)

| Policy | Required Scope |
|--------|---------------|
| `"RequireApiScope"` | `scope = "api"` |
| `"RequireAdminScope"` | `scope = "admin"` |
| `"RequireProfileScope"` | `scope = "profile"` |
| `"RequireEmailScope"` | `scope = "email"` |

### Usage in Endpoints

```csharp
// Group-level
app.MapGroup("api/v1/accounts").RequireAuthorization();

// Endpoint-level with policy
accounts.MapPost("/", handler).RequireAuthorization("AdminOnly");

// Inline policy
accounts.MapPost("/", handler)
    .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
```

---

## UserIdentityService

**File:** `src/Acme.Infrastructure/Auth/UserIdentityService.cs`

Scoped service that extracts user info from `HttpContext.User` claims:

```csharp
public class UserIdentityService(IHttpContextAccessor httpContextAccessor) : IUserIdentityService
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
    public string? UserName => httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;
    public string GetRole() => httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value
        ?? throw new InvalidOperationException("User roles missing.");
}
```

Claims used: `sub` (user ID), `name` (display name), `role` (user role).
