using Acme.Api.Extensions;
using Acme.AppHost.Middleware;
using Acme.Infrastructure.Extensions;
using Scalar.AspNetCore;

namespace Acme.AppHost.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the complete middleware pipeline for the application.
    /// Middleware order is critical - each middleware wraps the next one.
    /// </summary>
    /// <remarks>
    /// Pipeline flow:
    /// 1. Exception handling (captures all downstream exceptions)
    /// 2. Status code pages (converts error codes to ProblemDetails)
    /// 3. HTTPS redirection (enforce secure connections)
    /// 4. Logging/diagnostics (CorrelationId for tracing)
    /// 5. CORS (cross-origin before auth)
    /// 6. Rate limiting (throttle before auth)
    /// 7. Cookie extraction (prepare auth header)
    /// 8. Authentication/Authorization (security)
    /// 9. Security headers (response hardening)
    /// 10. Endpoints (business logic)
    /// </remarks>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // === Development-only features ===
        if (app.Environment.IsDevelopment())
        {
            // Swagger/OpenAPI generation (required for both Swagger UI and Scalar)
            app.UseSwagger();
            
            // Scalar UI - Modern OpenAPI documentation (recommended)
            app.MapScalarApiReference("/scalar", options =>
            {
                options
                    .WithTitle("Acme API")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
            });
            
            // Swagger UI (legacy, kept for compatibility)
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Acme API v1");
                options.RoutePrefix = "swagger";
                
                // UI improvements
                options.DefaultModelsExpandDepth(-1);
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
            });
        }
        else
        {
            // HSTS in production (HTTP Strict Transport Security)
            app.UseHsts();
        }

        // === Error Handling (must be first) ===
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        
        // === HTTPS Redirection (configurable for container environments) ===
        if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection"))
        {
            app.UseHttpsRedirection();
        }
        
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
        
        // === Cookie to Authorization Header (before authentication) ===
        app.UseMiddleware<CookieToHeaderMiddleware>();
        
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
