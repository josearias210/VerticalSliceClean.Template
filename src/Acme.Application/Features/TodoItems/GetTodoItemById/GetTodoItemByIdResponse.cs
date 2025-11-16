namespace Acme.Application.Features.TodoItems.GetTodoItemById;

public record GetTodoItemByIdResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? CompletedAt);
