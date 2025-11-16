using ErrorOr;
using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using MediatR;

namespace Acme.Application.Features.TodoItems.CreateTodoItem;

public class CreateTodoItemCommandHandler(
    IApplicationDbContext dbContext,
    IUserIdentityService userIdentityService)
    : IRequestHandler<CreateTodoItemCommand, ErrorOr<CreateTodoItemResponse>>
{
    private readonly IApplicationDbContext dbContext = dbContext;
    private readonly IUserIdentityService userIdentityService = userIdentityService;

    public async Task<ErrorOr<CreateTodoItemResponse>> Handle(
        CreateTodoItemCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = userIdentityService.GetUserId()!;

        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = currentUserId
        };

        dbContext.TodoItems.Add(todoItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateTodoItemResponse(
            todoItem.Id,
            todoItem.Title,
            todoItem.Description,
            todoItem.IsCompleted,
            todoItem.CreatedAt);
    }
}
