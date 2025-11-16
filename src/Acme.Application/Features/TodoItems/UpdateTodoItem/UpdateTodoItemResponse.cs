namespace Acme.Application.Features.TodoItems.UpdateTodoItem;

public record UpdateTodoItemResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? CompletedAt);
