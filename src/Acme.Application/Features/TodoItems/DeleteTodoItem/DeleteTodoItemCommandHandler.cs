using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Application.Features.TodoItems.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acme.Application.Features.TodoItems.DeleteTodoItem;

public class DeleteTodoItemCommandHandler(
    IApplicationDbContext dbContext,
    IUserIdentityService userIdentityService)
    : IRequestHandler<DeleteTodoItemCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext dbContext = dbContext;
    private readonly IUserIdentityService userIdentityService = userIdentityService;

    public async Task<ErrorOr<Success>> Handle(
        DeleteTodoItemCommand request,
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

        dbContext.TodoItems.Remove(todoItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
