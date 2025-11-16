using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.TodoItems.GetTodoItemById;
using Acme.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Acme.UnitTests.TodoItems;

public class GetTodoItemByIdQueryHandlerTests
{
    private readonly string testUserId = Guid.NewGuid().ToString();
    private readonly string otherUserId = Guid.NewGuid().ToString();

    [Fact]
    public async Task ShouldReturnTodoItemWhenOwnerRequests()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todo = new TodoItem
        {
            Id = todoId,
            Title = "Test todo",
            Description = "Description",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = testUserId
        };

        var todoList = new List<TodoItem> { todo };
        var mockDbSet = todoList.AsQueryable().BuildMockDbSet();

        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockDbSet.Object);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new GetTodoItemByIdQueryHandler(mockDbContext.Object, userIdentityService.Object);
        var query = new GetTodoItemByIdQuery(todoId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(todoId);
        result.Value.Title.Should().Be("Test todo");
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenTodoItemDoesNotExist()
    {
        // Arrange
        var todoList = new List<TodoItem>(); // empty list
        var mockDbSet = todoList.AsQueryable().BuildMockDbSet();

        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockDbSet.Object);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new GetTodoItemByIdQueryHandler(mockDbContext.Object, userIdentityService.Object);
        var query = new GetTodoItemByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("TodoItem.NotFound");
    }

    [Fact]
    public async Task ShouldReturnForbiddenWhenUserNotOwner()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todo = new TodoItem
        {
            Id = todoId,
            Title = "Someone else's todo",
            Description = "Private",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = otherUserId
        };

        var todoList = new List<TodoItem> { todo };
        var mockDbSet = todoList.AsQueryable().BuildMockDbSet();

        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockDbSet.Object);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new GetTodoItemByIdQueryHandler(mockDbContext.Object, userIdentityService.Object);
        var query = new GetTodoItemByIdQuery(todoId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("TodoItem.Forbidden");
    }
}
