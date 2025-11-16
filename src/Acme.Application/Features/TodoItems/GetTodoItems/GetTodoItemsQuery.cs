using ErrorOr;
using MediatR;

namespace Acme.Application.Features.TodoItems.GetTodoItems;

public record GetTodoItemsQuery(
    bool? IsCompleted = null
) : IRequest<ErrorOr<List<GetTodoItemsResponse>>>;
