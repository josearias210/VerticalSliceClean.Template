using ErrorOr;
using MediatR;

namespace Acme.Application.Features.TodoItems.GetTodoItemById;

public record GetTodoItemByIdQuery(Guid Id) : IRequest<ErrorOr<GetTodoItemByIdResponse>>;
