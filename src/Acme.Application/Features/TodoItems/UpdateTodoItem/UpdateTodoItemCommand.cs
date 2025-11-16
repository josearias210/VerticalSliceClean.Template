using ErrorOr;
using MediatR;

namespace Acme.Application.Features.TodoItems.UpdateTodoItem;

public record UpdateTodoItemCommand(
    Guid Id,
    string? Title,
    string? Description,
    bool? IsCompleted) : IRequest<ErrorOr<UpdateTodoItemResponse>>;
