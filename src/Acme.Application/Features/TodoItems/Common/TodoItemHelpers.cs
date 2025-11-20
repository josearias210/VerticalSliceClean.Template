using ErrorOr;
using Acme.Domain.Entities;
using Acme.Application.Shared;

namespace Acme.Application.Features.TodoItems.Common;

/// <summary>
/// Helper methods shared across TodoItem features to reduce code duplication.
/// </summary>
internal static class TodoItemHelpers
{
    /// <summary>
    /// Validates that the current user owns the todo item.
    /// Returns a Forbidden error if the user doesn't own the item.
    /// </summary>
    public static ErrorOr<Success> ValidateOwnership(TodoItem todoItem, string currentUserId)
    {
        if (todoItem.CreatedByAccountId != currentUserId)
        {
            return Error.Forbidden(
                code: ErrorCodes.TodoItem.Forbidden,
                description: "You are not authorized to access this todo item.");
        }

        return Result.Success;
    }

    /// <summary>
    /// Creates a NotFound error for a todo item.
    /// </summary>
    public static Error NotFoundError(Guid todoItemId)
    {
        return Error.NotFound(
            code: ErrorCodes.TodoItem.NotFound,
            description: $"Todo item with id '{todoItemId}' was not found");
    }
}
