namespace Acme.Application.Features.TodoItems.GetTodoItems;

public record GetTodoItemsResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? CompletedAt);
