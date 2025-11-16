using ErrorOr;
using Acme.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Acme.Application.Features.TodoItems.GetTodoItems;

public class GetTodoItemsQueryHandler(
    IApplicationDbContext dbContext,
    IUserIdentityService userIdentityService)
    : IRequestHandler<GetTodoItemsQuery, ErrorOr<List<GetTodoItemsResponse>>>
{
    private readonly IApplicationDbContext dbContext = dbContext;
    private readonly IUserIdentityService userIdentityService = userIdentityService;

    public async Task<ErrorOr<List<GetTodoItemsResponse>>> Handle(
        GetTodoItemsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = userIdentityService.GetUserId()!;

        var query = dbContext.TodoItems
            .Where(t => t.CreatedByAccountId == currentUserId);

        if (request.IsCompleted.HasValue)
        {
            query = query.Where(t => t.IsCompleted == request.IsCompleted.Value);
        }

        var todoItems = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new GetTodoItemsResponse(
                t.Id,
                t.Title,
                t.Description,
                t.IsCompleted,
                t.CreatedAt,
                t.CompletedAt))
            .ToListAsync(cancellationToken);

        return todoItems;
    }
}
