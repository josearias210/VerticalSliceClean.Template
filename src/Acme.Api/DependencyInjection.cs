namespace Acme.Api;

using Acme.Api.Exceptions;
using Acme.Api.Extensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddCustomizedProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddEndpoints(); // Register endpoints as keyed services

        // .NET 10: Configure JSON options for empty string → null conversion
        services.Configure<JsonOptions>(options =>
        {
            // Treat empty strings as null for nullable value types
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }
}
