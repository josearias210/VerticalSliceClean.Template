using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.TodoItems.DeleteTodoItem;
using Acme.Domain.Entities;
using Acme.UnitTests.Mocks;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Acme.UnitTests.TodoItems;

public class DeleteTodoItemCommandHandlerTests
{
    private readonly string testUserId = Guid.NewGuid().ToString();
    private readonly string otherUserId = Guid.NewGuid().ToString();

    /// <summary>
    /// Verifies that a TodoItem can be successfully deleted by its owner,
    /// removing it from the database.
    /// </summary>
    [Fact]
    public async Task ShouldDeleteTodoItemSuccessfully()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todo = new TodoItem
        {
            Id = todoId,
            Title = "Task to delete",
            Description = null,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = testUserId
        };

        var todoList = new List<TodoItem> { todo };
        var mockDbSet = todoList.AsQueryable().BuildMockDbSet();
        
        TodoItem? removedItem = null;
        mockDbSet.Setup(m => m.Remove(It.IsAny<TodoItem>()))
            .Callback<TodoItem>(item => removedItem = item);

        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockDbSet.Object);
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new DeleteTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new DeleteTodoItemCommand(todoId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        
        removedItem.Should().NotBeNull();
        removedItem!.Id.Should().Be(todoId);
        mockDbSet.Verify(m => m.Remove(It.IsAny<TodoItem>()), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that attempting to delete a non-existent TodoItem returns a NotFound error.
    /// </summary>
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

        var handler = new DeleteTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new DeleteTodoItemCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("TodoItem.NotFound");
    }

    /// <summary>
    /// Verifies that a user cannot delete a TodoItem that belongs to another user,
    /// returning a Forbidden error to enforce ownership permissions.
    /// </summary>
    [Fact]
    public async Task ShouldReturnForbiddenWhenUserNotOwner()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todo = new TodoItem
        {
            Id = todoId,
            Title = "Someone else's todo",
            Description = null,
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

        var handler = new DeleteTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new DeleteTodoItemCommand(todoId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("TodoItem.Forbidden");
    }
}
