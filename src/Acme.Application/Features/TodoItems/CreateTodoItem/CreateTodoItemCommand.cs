using ErrorOr;
using MediatR;

namespace Acme.Application.Features.TodoItems.CreateTodoItem;

public record CreateTodoItemCommand(
    string Title,
    string? Description
) : IRequest<ErrorOr<CreateTodoItemResponse>>;
