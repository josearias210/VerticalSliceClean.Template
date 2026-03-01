namespace Acme.Host.Extensions;

using Acme.Api.Extensions;
using Acme.Host.Middleware;
using Acme.Infrastructure.Extensions;
using Scalar.AspNetCore;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // === Development-only features ===
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/scalar", options =>
            {
                options
                    .WithTitle("Acme API")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }
        else
        {
            // HSTS in production (HTTP Strict Transport Security)
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        // === Error Handling (must be first) ===
        app.UseExceptionHandler();
        app.UseStatusCodePages();
                
        // === Logging and Diagnostics ===
        // Serilog handles HTTP request logging automatically
        app.UseMiddleware<CorrelationIdMiddleware>();
        
        // === CORS (before authentication) ===
        app.UseConfiguredCors();
        
        // === Rate Limiting (before authentication to prevent brute force) ===
        // Skip rate limiting in test environment
        if (app.Configuration["TestEnvironment"] != "true")
        {
            app.UseRateLimiter();
        }
                
        // === Authentication & Authorization === 
        app.UseAuthentication();
        app.UseAuthorization();
        
        // === Security Headers (response hardening) ===
        app.UseDefaultSecurityHeaders(app.Environment);
        
        // === Endpoints ===
        app.MapDefaultHealthChecks();
        app.MapRoutes();

        // === Graceful Shutdown Handling ===
        ConfigureGracefulShutdown(app);

        return app;
    }

    /// <summary>
    /// Configures graceful shutdown behavior for the application.
    /// Ensures logs are flushed and resources cleaned up on shutdown.
    /// </summary>
    private static void ConfigureGracefulShutdown(WebApplication app)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Shutdown");

        lifetime.ApplicationStopping.Register(() =>
        {
            logger.LogInformation("Application is shutting down gracefully...");
        });

        lifetime.ApplicationStopped.Register(() =>
        {
            logger.LogInformation("Application has stopped.");
        });
    }
}
