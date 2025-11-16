using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Application.Features.TodoItems.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acme.Application.Features.TodoItems.UpdateTodoItem;

public class UpdateTodoItemCommandHandler(
    IApplicationDbContext dbContext,
    IUserIdentityService userIdentityService)
    : IRequestHandler<UpdateTodoItemCommand, ErrorOr<UpdateTodoItemResponse>>
{
    private readonly IApplicationDbContext dbContext = dbContext;
    private readonly IUserIdentityService userIdentityService = userIdentityService;

    public async Task<ErrorOr<UpdateTodoItemResponse>> Handle(
        UpdateTodoItemCommand request,
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

        // Update only provided fields
        if (request.Title is not null)
        {
            todoItem.Title = request.Title;
        }

        if (request.Description is not null)
        {
            todoItem.Description = request.Description;
        }

        if (request.IsCompleted.HasValue)
        {
            todoItem.IsCompleted = request.IsCompleted.Value;
            if (request.IsCompleted.Value && todoItem.CompletedAt is null)
            {
                todoItem.CompletedAt = DateTime.UtcNow;
            }
            else if (!request.IsCompleted.Value)
            {
                todoItem.CompletedAt = null;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateTodoItemResponse(
            todoItem.Id,
            todoItem.Title,
            todoItem.Description,
            todoItem.IsCompleted,
            todoItem.CreatedAt,
            todoItem.CompletedAt);
    }
}
