namespace Acme.Application.Behaviors;

using ErrorOr;
using FluentValidation;
using MediatR;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        // Ejecuta todos los validadores en paralelo
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        // Use FluentValidation ErrorCode if available, otherwise PropertyName
        // This allows custom error codes like "EmailAlreadyExists" instead of just "Email"
        // Reemplaza la llamada a EndsWith por una versión que especifique StringComparison.Ordinal
        var errors = failures.ConvertAll(f => Error.Validation(
            code: (f.ErrorCode.EndsWith("Validator", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(f.ErrorCode)) ? f.PropertyName : f.ErrorCode,
            description: f.ErrorMessage
        ));

        return (dynamic)errors;
    }
}
