using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Acme.Infrastructure.Extensions;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseDefaultSecurityHeaders(this IApplicationBuilder app, IHostEnvironment env)
    {
        app.Use(async (ctx, next) =>
        {
            var headers = ctx.Response.Headers;

            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers.XXSSProtection = "0";

            headers.ContentSecurityPolicy =
                "default-src 'self'; " +
                "img-src 'self' data:; " +
                "style-src 'self'; " +
                "script-src 'self'; " +
                "connect-src 'self'; " +
                "font-src 'self'";

            if (!env.IsDevelopment())
            {
                headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains; preload";
            }

            await next();
        });

        // Disable CSP for documentation endpoints (Scalar, OpenAPI)
        // These tools require inline scripts and styles to function
        app.UseWhen(ctx => 
            ctx.Request.Path.StartsWithSegments("/scalar") ||
            ctx.Request.Path.StartsWithSegments("/openapi"),
            branch =>
        {
            branch.Use(async (ctx, next) =>
            {
                ctx.Response.Headers.Remove("Content-Security-Policy");
                await next();
            });
        });

        return app;
    }
}
