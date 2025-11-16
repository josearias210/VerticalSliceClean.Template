using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Api.Extensions;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}

public static class EndpointsRegistration
{
    /// <summary>
    /// Registers all IEndpoint implementations using keyed services for better performance.
    /// Avoids Activator.CreateInstance reflection on every call.
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var asm = typeof(EndpointsRegistration).Assembly;
        var endpointTypes = asm.DefinedTypes
            .Where(t => !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            services.AddKeyedSingleton(typeof(IEndpoint), type.Name, type);
        }

        return services;
    }

    /// <summary>
    /// Maps all registered IEndpoint implementations to the route builder.
    /// Uses keyed services for dependency resolution.
    /// </summary>
    public static IEndpointRouteBuilder MapRoutes(this IEndpointRouteBuilder app)
    {
        var asm = typeof(EndpointsRegistration).Assembly;
        var endpointTypes = asm.DefinedTypes
            .Where(t => !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            var endpoint = app.ServiceProvider.GetRequiredKeyedService<IEndpoint>(type.Name);
            endpoint.Map(app);
        }
        
        return app;
    }
}
