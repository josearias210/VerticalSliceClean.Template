namespace Acme.Api.Extensions;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}

public static class EndpointsRegistration
{
    /// <summary>
    /// Registers all IEndpoint implementations as standard Singletons.
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var asm = typeof(EndpointsRegistration).Assembly;
        var endpointTypes = asm.DefinedTypes.Where(t => !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            services.AddSingleton(typeof(IEndpoint), type);
        }

        return services;
    }

    /// <summary>
    /// Maps all registered IEndpoint implementations using the DI container.
    /// Avoids a second reflection pass.
    /// </summary>
    public static IEndpointRouteBuilder MapRoutes(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetServices<IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.Map(app);
        }
        
        return app;
    }
}
