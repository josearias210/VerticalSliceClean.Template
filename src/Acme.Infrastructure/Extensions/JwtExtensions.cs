using Acme.Domain.Entities;
using Acme.Infrastructure.Persistence.EF;
using Acme.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Acme.Infrastructure.Extensions;

public static class JwtExtensions
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

    /// <summary>
    /// Configures JWT Bearer authentication with token validation.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var sp = services.BuildServiceProvider();
            var jwtSettings = sp.GetRequiredService<IOptions<JwtSettings>>().Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // No tolerance for expired tokens
                
                // Map custom role claim to standard role claim type
                RoleClaimType = "Role"
            };

            options.SaveToken = true;

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Log authentication failures if needed
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Customize 401 responses if needed
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
