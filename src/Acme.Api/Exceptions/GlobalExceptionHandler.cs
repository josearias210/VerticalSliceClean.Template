namespace Acme.Api.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal sealed class GlobalExceptionHandler(IHostEnvironment env, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail, logLevel) = MapException(exception);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = env.IsDevelopment() ? exception.ToString() : detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path,
        };
        if (env.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
            if (exception.InnerException != null)
            {
                problem.Extensions["innerException"] = exception.InnerException.Message;
            }
        }
        LogException(status, title, exception, logLevel);
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title, string Detail, LogLevel LogLevel) MapException(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            return (StatusCodes.Status409Conflict, "Concurrency Conflict", "The resource was modified by another user. Please refresh and try again.", LogLevel.Warning);
        }
        if (exception is DbUpdateException dbEx)
        {
            return IsDuplicateKeyException(dbEx)
                ? (StatusCodes.Status409Conflict, "Duplicate Resource", "A resource with the same key already exists.", LogLevel.Warning)
                : (StatusCodes.Status500InternalServerError, "Database Error", "An error occurred while updating the database.", LogLevel.Error);
        }
        return exception switch
        {
            ArgumentNullException argNull => (StatusCodes.Status400BadRequest, "Bad Request", $"Required parameter '{argNull.ParamName}' is missing.", LogLevel.Warning),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, "Invalid Argument", argEx.Message, LogLevel.Warning),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "The requested resource was not found.", LogLevel.Information),
            InvalidOperationException invalidOp => (StatusCodes.Status400BadRequest, "Invalid Operation", invalidOp.Message, LogLevel.Warning),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", "You don't have permission to access this resource.", LogLevel.Warning),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "Request Timeout", "The operation took too long to complete.", LogLevel.Warning),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Request Cancelled", "The request was cancelled.", LogLevel.Information),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred. Please try again later.", LogLevel.Error)
        };
    }

    private static bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601);
    }

    private void LogException(int status, string title, Exception exception, LogLevel logLevel)
    {
        logger.Log(logLevel, exception, "Unhandled exception: {StatusCode} {Title} | Type: {ExceptionType} | Message: {Message}", status, title, exception.GetType().Name, exception.Message);
    }
}
