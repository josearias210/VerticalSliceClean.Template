namespace Acme.Api.Extensions;

using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static class TypedResultsExtensions
{
    public static Results<Ok<TValue>, ProblemHttpResult, NotFound, Conflict> ToTypedResult<TValue>(this ErrorOr<TValue> result)
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

    public static Results<Created<TValue>, ProblemHttpResult, Conflict> ToCreatedResult<TValue>(this ErrorOr<TValue> result, string uri)
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

    public static Results<NoContent, ProblemHttpResult, NotFound> ToNoContentResult<TValue>(this ErrorOr<TValue> result)
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

    public static Results<Ok<TValue>, ProblemHttpResult, NotFound, UnauthorizedHttpResult> ToAuthResult<TValue>(this ErrorOr<TValue> result)
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

    private static ProblemHttpResult CreateProblem(List<Error> errors, Error firstError, int statusCode = StatusCodes.Status400BadRequest)
    {
        var errorCodes = errors.Select(e => e.Code).Distinct().ToList();
        if (errors.Count > 1 && errors.All(e => e.Type == ErrorType.Validation))
        {
            return TypedResults.Problem(
                statusCode: statusCode,
                title: "Validation Error",
                detail: firstError.Description,
                extensions: new Dictionary<string, object?> { ["errors"] = errorCodes }
            );
        }
        return TypedResults.Problem(
            statusCode: statusCode,
            title: firstError.Code,
            extensions: new Dictionary<string, object?> { ["errors"] = errorCodes }
        );
    }
}
