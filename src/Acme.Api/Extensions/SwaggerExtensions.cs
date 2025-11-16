using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Acme.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // API Info
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Acme API",
                Version = "v1",
                Description = "RESTful API with JWT authentication using httpOnly cookies, " +
                              "vertical slice architecture, and ErrorOr pattern for error handling.",
                Contact = new OpenApiContact
                {
                    Name = "Acme",
                    Email = "contact@acme.com",
                    Url = new Uri("https://github.com/josearias210/Acme")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // XML Comments (if available)
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // JWT Bearer Authentication
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT Authorization header using Bearer scheme. " +
                              "Enter 'Bearer' [space] and then your token in the text input below. " +
                              "Example: 'Bearer eyJhbGciOi...'",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { bearerScheme, new List<string>() }
            });

            // Cookie Authentication (for refresh tokens)
            var cookieScheme = new OpenApiSecurityScheme
            {
                Name = "refreshToken",
                Description = "Refresh token stored in httpOnly cookie. " +
                              "Automatically sent by browser. Used for /refresh endpoint.",
                In = ParameterLocation.Cookie,
                Type = SecuritySchemeType.ApiKey,
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Cookie"
                }
            };

            options.AddSecurityDefinition("Cookie", cookieScheme);

            // Common response schemas
            options.MapType<ProblemDetails>(() => new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["type"] = new OpenApiSchema { Type = "string", Description = "A URI reference that identifies the problem type" },
                    ["title"] = new OpenApiSchema { Type = "string", Description = "A short, human-readable summary of the problem" },
                    ["status"] = new OpenApiSchema { Type = "integer", Description = "The HTTP status code" },
                    ["detail"] = new OpenApiSchema { Type = "string", Description = "A human-readable explanation specific to this occurrence" },
                    ["instance"] = new OpenApiSchema { Type = "string", Description = "A URI reference that identifies the specific occurrence" },
                    ["traceId"] = new OpenApiSchema { Type = "string", Description = "Trace ID for correlation with logs" },
                    ["correlationId"] = new OpenApiSchema { Type = "string", Description = "Correlation ID for distributed tracing" },
                    ["code"] = new OpenApiSchema { Type = "string", Description = "Application-specific error code for client handling" }
                },
                Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["type"] = new Microsoft.OpenApi.Any.OpenApiString("https://httpstatuses.com/401"),
                    ["title"] = new Microsoft.OpenApi.Any.OpenApiString("Unauthorized"),
                    ["status"] = new Microsoft.OpenApi.Any.OpenApiInteger(401),
                    ["detail"] = new Microsoft.OpenApi.Any.OpenApiString("Invalid credentials"),
                    ["code"] = new Microsoft.OpenApi.Any.OpenApiString("Auth.InvalidCredentials"),
                    ["traceId"] = new Microsoft.OpenApi.Any.OpenApiString("00-abc123..."),
                    ["correlationId"] = new Microsoft.OpenApi.Any.OpenApiString("xyz789...")
                }
            });
        });

        return services;
    }
}
