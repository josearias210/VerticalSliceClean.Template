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
        var todos = app.MapV1Group("todos")
            .RequireAuthorization();

        todos.MapPost("", async (ISender sender, [FromBody] CreateTodoItemCommand command, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToCreatedResult($"/api/v1/todos"))
            .WithMetadata("CreateTodoItem", "Create a new todo item", "Creates a new todo item for the authenticated user");

        todos.MapGet("", async (ISender sender, [FromQuery] bool? isCompleted, CancellationToken ct) =>
                (await sender.Send(new GetTodoItemsQuery(isCompleted), ct)).ToTypedResult())
            .WithMetadata("GetTodoItems", "Get all todo items", "Gets all todo items for the authenticated user with optional filtering");

        todos.MapGet("{id:guid}", async (ISender sender, Guid id, CancellationToken ct) =>
                (await sender.Send(new GetTodoItemByIdQuery(id), ct)).ToTypedResult())
            .WithMetadata("GetTodoItemById", "Get todo item by id", "Gets a specific todo item by its id");

        todos.MapPut("{id:guid}", async (ISender sender, Guid id, [FromBody] UpdateTodoItemRequest request, CancellationToken ct) =>
            {
                var command = new UpdateTodoItemCommand(id, request.Title, request.Description, request.IsCompleted);
                return (await sender.Send(command, ct)).ToTypedResult();
            })
            .WithMetadata("UpdateTodoItem", "Update a todo item", "Updates an existing todo item. Only provided fields will be updated.");

        todos.MapDelete("{id:guid}", async (ISender sender, Guid id, CancellationToken ct) =>
                (await sender.Send(new DeleteTodoItemCommand(id), ct)).ToNoContentResult())
            .WithMetadata("DeleteTodoItem", "Delete a todo item", "Deletes an existing todo item");
    }

    // Request model for update (separates route parameter from body)
    private sealed record UpdateTodoItemRequest(string? Title, string? Description, bool? IsCompleted);
}
