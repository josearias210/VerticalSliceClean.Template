namespace Acme.Application;

using Acme.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register most validators as Singleton for better performance (they're stateless)
        services.AddValidatorsFromAssembly(
            assembly,
            lifetime: ServiceLifetime.Singleton/*,
            filter: result => result.ValidatorType != typeof(RegisterAccountCommandValidator)*/);

        // RegisterAccountCommandValidator needs Scoped because it depends on UserManager (Scoped)
        //services.AddScoped<IValidator<RegisterAccountCommand>>(sp =>new RegisterAccountCommandValidator(sp.GetRequiredService<UserManager<Account>>()));

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        return services;
    }
}
