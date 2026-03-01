namespace Acme.Infrastructure.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

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

            // Check if we should skip CSP for this request (e.g. docs)
            bool skipCsp = ctx.Request.Path.StartsWithSegments("/scalar") ||
                           ctx.Request.Path.StartsWithSegments("/openapi");

            if (!skipCsp)
            {
                headers.ContentSecurityPolicy =
                    "default-src 'self'; " +
                    "img-src 'self' data:; " +
                    "style-src 'self'; " +
                    "script-src 'self'; " +
                    "connect-src 'self'; " +
                    "font-src 'self'";
            }

            if (!env.IsDevelopment())
            {
                headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains; preload";
            }

            await next();
        });

        return app;
    }
}
