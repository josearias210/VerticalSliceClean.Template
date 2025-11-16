namespace Acme.Application.Features.TodoItems.CreateTodoItem;

public record CreateTodoItemResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt);
