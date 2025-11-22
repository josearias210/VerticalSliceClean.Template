using Acme.Domain.Entities;
using Acme.Infrastructure.Persistence.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Acme.Infrastructure.Extensions;

public static class IdentityExtensions
{
    /// <summary>
    /// Configures ASP.NET Core Identity with custom Account entity.
    /// </summary>
    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services
            .AddIdentityCore<Account>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false; // Will be true after Email Confirmation implementation

                // Password policies
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // Account lockout configuration
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddSignInManager()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        return services;
    }

    public static IServiceCollection AddOpenIddictAuth(this IServiceCollection services, IHostEnvironment environment)
    {
        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token")
                       .SetRevocationEndpointUris("/connect/revoke");

                options.AllowPasswordFlow()
                       .AllowRefreshTokenFlow();

                options.RegisterScopes(
                    Scopes.OpenId,
                    Scopes.Profile,
                    Scopes.OfflineAccess,
                    Scopes.Email,
                    "api"
                );

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));
                options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(5));

                // Certificate configuration based on environment
                if (environment.IsDevelopment())
                {
                    // Development: Use ephemeral certificates
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                    
                    options.DisableAccessTokenEncryption();
                }
                else
                {
                    // Production: Load certificates from configuration
                    var openIddictSettings = services.BuildServiceProvider()
                        .GetRequiredService<Microsoft.Extensions.Options.IOptions<Settings.OpenIddictSettings>>()
                        .Value;

                    if (!string.IsNullOrEmpty(openIddictSettings.EncryptionCertificatePath) &&
                        !string.IsNullOrEmpty(openIddictSettings.SigningCertificatePath))
                    {
                        var encryptionCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            openIddictSettings.EncryptionCertificatePath,
                            openIddictSettings.CertificatePassword);

                        var signingCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            openIddictSettings.SigningCertificatePath,
                            openIddictSettings.CertificatePassword);

                        options.AddEncryptionCertificate(encryptionCert)
                               .AddSigningCertificate(signingCert);
                    }
                    else
                    {
                        throw new InvalidOperationException("OpenIddict certificate paths must be configured in production. Set 'OpenIddict:EncryptionCertificatePath' and 'OpenIddict:SigningCertificatePath' in configuration.");
                    }
                }

                options.UseAspNetCore();

                options.AddEventHandler<OpenIddict.Server.OpenIddictServerEvents.HandleTokenRequestContext>(builder => builder.UseScopedHandler<Auth.OpenIddict.PasswordGrantHandler>());
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();

        return services;
    }
}
