using Acme.Api.Extensions;
using Acme.Application.Features.TodoItems.CreateTodoItem;
using Acme.Application.Features.TodoItems.DeleteTodoItem;
using Acme.Application.Features.TodoItems.GetTodoItemById;
using Acme.Application.Features.TodoItems.GetTodoItems;
using Acme.Application.Features.TodoItems.UpdateTodoItem;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Acme.Api.Endpoints;

public sealed class TodoItemsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("api/v1/todos")
            .RequireAuthorization();

        todos.MapPost("/", async (ISender sender, [FromBody] CreateTodoItemCommand command, CancellationToken ct) => (await sender.Send(command, ct)).ToCreatedResult($"/api/v1/todos"));

        todos.MapGet("/", async (ISender sender, [FromQuery] bool? isCompleted, CancellationToken ct) => (await sender.Send(new GetTodoItemsQuery(isCompleted), ct)).ToTypedResult());
        todos.MapGet("{id:guid}", async (ISender sender, Guid id, CancellationToken ct) => (await sender.Send(new GetTodoItemByIdQuery(id), ct)).ToTypedResult());

        todos.MapPut("{id:guid}", async (ISender sender, Guid id, [FromBody] UpdateTodoItemCommand command, CancellationToken ct) => (await sender.Send(command, ct)).ToTypedResult());

        todos.MapDelete("{id:guid}", async (ISender sender, Guid id, CancellationToken ct) => (await sender.Send(new DeleteTodoItemCommand(id), ct)).ToNoContentResult());
    }
}
