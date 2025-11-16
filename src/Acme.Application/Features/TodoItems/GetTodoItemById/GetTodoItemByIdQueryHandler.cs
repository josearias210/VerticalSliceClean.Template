using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Application.Features.TodoItems.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acme.Application.Features.TodoItems.GetTodoItemById;

public class GetTodoItemByIdQueryHandler(
    IApplicationDbContext dbContext,
    IUserIdentityService userIdentityService)
    : IRequestHandler<GetTodoItemByIdQuery, ErrorOr<GetTodoItemByIdResponse>>
{
    private readonly IApplicationDbContext dbContext = dbContext;
    private readonly IUserIdentityService userIdentityService = userIdentityService;

    public async Task<ErrorOr<GetTodoItemByIdResponse>> Handle(
        GetTodoItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = userIdentityService.GetUserId()!;

        var todoItem = await dbContext.TodoItems
            .Where(t => t.Id == request.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (todoItem is null)
            return TodoItemHelpers.NotFoundError(request.Id);

        var ownershipCheck = TodoItemHelpers.ValidateOwnership(todoItem, currentUserId);
        if (ownershipCheck.IsError)
            return ownershipCheck.Errors;

        return new GetTodoItemByIdResponse(
            todoItem.Id,
            todoItem.Title,
            todoItem.Description,
            todoItem.IsCompleted,
            todoItem.CreatedAt,
            todoItem.CompletedAt);
    }
}
