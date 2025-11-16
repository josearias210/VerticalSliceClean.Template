using ErrorOr;
using MediatR;

namespace Acme.Application.Features.TodoItems.DeleteTodoItem;

public record DeleteTodoItemCommand(Guid Id) : IRequest<ErrorOr<Success>>;
