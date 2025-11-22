using Acme.Application.Abstractions;
using Acme.Infrastructure.Auth;
using Acme.Infrastructure.Extensions;
using Acme.Infrastructure.Persistence.EF;
using Acme.Infrastructure.Services;
using Acme.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Acme.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();

        // === Configuration Settings (with validation) ===
        services.AddOptions<DatabaseSettings>()
            .BindConfiguration("Database")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AdminUserSettings>()
            .BindConfiguration("Admin")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .BindConfiguration("JwtSettings")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeqSettings>()
            .BindConfiguration("Logging:Seq")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OpenTelemetrySettings>()
            .BindConfiguration("OpenTelemetry")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CorsSettings>()
            .BindConfiguration("Cors")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // === Identity & Authentication ===
        services.AddIdentity();
        services.AddOpenIddictAuth(configuration, environment);
        services.AddAuthorizationPolicies();

        // === Authentication Services ===
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        
        // === Communication Services ===
        services.AddScoped<IEmailService, EmailService>();

        // === Database Services ===
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    // Retry policy for transient failures (production resilience)
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    
                    // Command timeout (30 seconds default)
                    sqlOptions.CommandTimeout(30);
                    
                    // Migrations history table configuration
                    sqlOptions.MigrationsHistoryTable(
                        ApplicationDbContext.MigrationsHistoryTable,
                        ApplicationDbContext.Schema);
                });

            // Development-only: Enable detailed errors and sensitive data logging
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseOpenIddict();
        });

        // === Telemetry ===
        var otlpSettings = services.BuildServiceProvider().GetRequiredService<IOptions<OpenTelemetrySettings>>().Value;
        services.AddOpenTelemetryInstrumentation(otlpSettings);

        // === Cross-cutting Concerns ===
        services.AddDefaultHealthChecks();
        var corsOptionsMonitor = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<CorsSettings>>();
        services.AddConfiguredCors(corsOptionsMonitor);

        return services;
    }
}
