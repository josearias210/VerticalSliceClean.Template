using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Acme.Api.Extensions;

/// <summary>
/// Extension methods to convert ErrorOr results to ASP.NET Core Typed Results.
/// Provides compile-time type safety and better OpenAPI generation.
/// </summary>
public static class TypedResultsExtensions
{
    /// <summary>
    /// Converts ErrorOr to Results with Ok/ValidationProblem/NotFound/Conflict.
    /// Common for GET/POST endpoints that can return validation errors, not found, or conflicts.
    /// </summary>
    public static Results<Ok<TValue>, ValidationProblem, NotFound, Conflict> ToTypedResult<TValue>(
        this ErrorOr<TValue> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateValidationProblem(result.Errors, firstError),
                ErrorType.NotFound => TypedResults.NotFound(),
                ErrorType.Conflict => TypedResults.Conflict(),
                _ => CreateValidationProblem(result.Errors, firstError)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    /// <summary>
    /// Converts ErrorOr to Results with Created/ValidationProblem/Conflict.
    /// Specific for POST endpoints that create resources.
    /// </summary>
    public static Results<Created<TValue>, ValidationProblem, Conflict> ToCreatedResult<TValue>(
        this ErrorOr<TValue> result,
        string uri)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateValidationProblem(result.Errors, firstError),
                ErrorType.Conflict => TypedResults.Conflict(),
                _ => CreateValidationProblem(result.Errors, firstError)
            };
        }

        return TypedResults.Created(uri, result.Value);
    }

    /// <summary>
    /// Converts ErrorOr to Results with NoContent/ValidationProblem/NotFound.
    /// Specific for DELETE/PUT endpoints that don't return content.
    /// </summary>
    public static Results<NoContent, ValidationProblem, NotFound> ToNoContentResult<TValue>(
        this ErrorOr<TValue> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateValidationProblem(result.Errors, firstError),
                ErrorType.NotFound => TypedResults.NotFound(),
                _ => CreateValidationProblem(result.Errors, firstError)
            };
        }

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Converts ErrorOr to Results with Ok/ValidationProblem/NotFound/Unauthorized.
    /// Specific for authentication endpoints.
    /// </summary>
    public static Results<Ok<TValue>, ValidationProblem, NotFound, UnauthorizedHttpResult> ToAuthResult<TValue>(
        this ErrorOr<TValue> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateValidationProblem(result.Errors, firstError),
                ErrorType.NotFound => TypedResults.NotFound(),
                ErrorType.Unauthorized => TypedResults.Unauthorized(),
                _ => CreateValidationProblem(result.Errors, firstError)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    /// <summary>
    /// Creates a ValidationProblem from ErrorOr errors.
    /// Groups multiple validation errors by their code (field name).
    /// </summary>
    private static ValidationProblem CreateValidationProblem(List<Error> errors, Error firstError)
    {
        // If multiple validation errors, group by code
        if (errors.Count > 1 && errors.All(e => e.Type == ErrorType.Validation))
        {
            return TypedResults.ValidationProblem(
                errors.ToDictionary(
                    e => e.Code,
                    e => new[] { e.Description }
                ),
                title: "Validation Error",
                detail: firstError.Description
            );
        }

        // Single error or non-validation errors
        return TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { ["Error"] = [firstError.Description] },
            title: firstError.Code
        );
    }
}

