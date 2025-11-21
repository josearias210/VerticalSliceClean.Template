// -----------------------------------------------------------------------
// <copyright file="AuthorizationPoliciesExtensions.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Acme.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring authorization policies.
/// </summary>
public static class AuthorizationPoliciesExtensions
{
    /// <summary>
    /// Adds custom authorization policies for role-based and claim-based access control.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Role-based policies
            .AddPolicy("AdminOnly", policy =>
                 policy.RequireRole("Admin", "Developer"))

            .AddPolicy("CanManageProducts", policy => 
                policy.RequireRole("Admin", "ProductManager"))
            
            .AddPolicy("CanManageUsers", policy => 
                policy.RequireRole("Admin"))
            
            // Claim-based policies
            .AddPolicy("EmailVerified", policy => 
                policy.RequireClaim("email_verified", "true"))
            
            .AddPolicy("MfaEnabled", policy => 
                policy.RequireClaim("amr", "mfa"));

        return services;
    }
}
