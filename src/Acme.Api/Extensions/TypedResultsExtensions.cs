namespace Acme.Api.Extensions;

using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Extension methods to convert ErrorOr results to ASP.NET Core Typed Results.
/// Provides compile-time type safety and better OpenAPI generation.
/// </summary>
public static class TypedResultsExtensions
{
    /// <summary>
    /// Converts ErrorOr to Results with Ok/Problem/NotFound/Conflict.
    /// Common for GET/POST endpoints that can return validation errors, not found, or conflicts.
    /// </summary>
    public static Results<Ok<TValue>, ProblemHttpResult, NotFound, Conflict> ToTypedResult<TValue>(
        this ErrorOr<TValue> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateProblem(result.Errors, firstError),
                ErrorType.NotFound => TypedResults.NotFound(),
                ErrorType.Conflict => CreateProblem(result.Errors, firstError, StatusCodes.Status409Conflict),
                _ => CreateProblem(result.Errors, firstError)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    /// <summary>
    /// Converts ErrorOr to Results with Created/Problem/Conflict.
    /// Specific for POST endpoints that create resources.
    /// </summary>
    public static Results<Created<TValue>, ProblemHttpResult, Conflict> ToCreatedResult<TValue>(
        this ErrorOr<TValue> result,
        string uri)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateProblem(result.Errors, firstError),
                ErrorType.Conflict => CreateProblem(result.Errors, firstError, StatusCodes.Status409Conflict),
                _ => CreateProblem(result.Errors, firstError)
            };
        }

        return TypedResults.Created(uri, result.Value);
    }

    /// <summary>
    /// Converts ErrorOr to Results with NoContent/Problem/NotFound.
    /// Specific for DELETE/PUT endpoints that don't return content.
    /// </summary>
    public static Results<NoContent, ProblemHttpResult, NotFound> ToNoContentResult<TValue>(
        this ErrorOr<TValue> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateProblem(result.Errors, firstError),
                ErrorType.NotFound => TypedResults.NotFound(),
                _ => CreateProblem(result.Errors, firstError)
            };
        }

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Converts ErrorOr to Results with Ok/Problem/NotFound/Unauthorized.
    /// Specific for authentication endpoints.
    /// </summary>
    public static Results<Ok<TValue>, ProblemHttpResult, NotFound, UnauthorizedHttpResult> ToAuthResult<TValue>(
        this ErrorOr<TValue> result)
    {
        if (result.IsError)
        {
            var firstError = result.FirstError;
            
            return firstError.Type switch
            {
                ErrorType.Validation => CreateProblem(result.Errors, firstError),
                ErrorType.NotFound => TypedResults.NotFound(),
                ErrorType.Unauthorized => TypedResults.Unauthorized(),
                _ => CreateProblem(result.Errors, firstError)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    /// <summary>
    /// Creates a ProblemHttpResult from ErrorOr errors.
    /// Returns errors as a list of codes in the 'errors' extension field.
    /// </summary>
    private static ProblemHttpResult CreateProblem(List<Error> errors, Error firstError, int statusCode = StatusCodes.Status400BadRequest)
    {
        var errorCodes = errors.Select(e => e.Code).Distinct().ToList();

        // If multiple validation errors, use a generic title
        if (errors.Count > 1 && errors.All(e => e.Type == ErrorType.Validation))
        {
            return TypedResults.Problem(
                statusCode: statusCode,
                title: "Validation Error",
                detail: firstError.Description,
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = errorCodes
                }
            );
        }

        // Single error or non-validation errors
        return TypedResults.Problem(
            statusCode: statusCode,
            title: firstError.Code,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = errorCodes
            }
        );
    }
}

