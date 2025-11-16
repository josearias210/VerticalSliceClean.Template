using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.TodoItems.UpdateTodoItem;
using Acme.Domain.Entities;
using Acme.UnitTests.Mocks;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Acme.UnitTests.TodoItems;

public class UpdateTodoItemCommandHandlerTests
{
    private readonly string testUserId = Guid.NewGuid().ToString();
    private readonly string otherUserId = Guid.NewGuid().ToString();

    /// <summary>
    /// Verifies that a TodoItem's title can be updated successfully while preserving
    /// other fields that are not being modified.
    /// </summary>
    [Fact]
    public async Task ShouldUpdateTitleSuccessfully()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todo = new TodoItem
        {
            Id = todoId,
            Title = "Original title",
            Description = "Original description",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = testUserId
        };

        var todoList = new List<TodoItem> { todo };
        var mockDbSet = todoList.AsQueryable().BuildMockDbSet();

        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockDbSet.Object);
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new UpdateTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new UpdateTodoItemCommand(todoId, "Updated title", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Title.Should().Be("Updated title");
        result.Value.Description.Should().Be("Original description"); // unchanged
        
        todo.Title.Should().Be("Updated title"); // verify entity was modified
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that marking a TodoItem as completed sets the IsCompleted flag
    /// and automatically records the completion timestamp.
    /// </summary>
    [Fact]
    public async Task ShouldMarkAsCompletedAndSetCompletedAt()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todo = new TodoItem
        {
            Id = todoId,
            Title = "Task",
            Description = null,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = testUserId
        };

        var todoList = new List<TodoItem> { todo };
        var mockDbSet = todoList.AsQueryable().BuildMockDbSet();

        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockDbSet.Object);
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new UpdateTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new UpdateTodoItemCommand(todoId, null, null, true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.IsCompleted.Should().BeTrue();
        result.Value.CompletedAt.Should().NotBeNull();
        result.Value.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        
        todo.CompletedAt.Should().NotBeNull();
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that marking a completed TodoItem as incomplete clears the CompletedAt
    /// timestamp and sets IsCompleted to false.
    /// </summary>
    [Fact]
    public async Task ShouldClearCompletedAtWhenMarkingAsIncomplete()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow.AddHours(-2);
        var todo = new TodoItem
        {
            Id = todoId,
            Title = "Completed task",
            Description = null,
            IsCompleted = true,
            CompletedAt = completedAt,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedByAccountId = testUserId
        };

        var todoList = new List<TodoItem> { todo };
        var mockDbSet = todoList.AsQueryable().BuildMockDbSet();

        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockDbSet.Object);
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new UpdateTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new UpdateTodoItemCommand(todoId, null, null, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.IsCompleted.Should().BeFalse();
        result.Value.CompletedAt.Should().BeNull();
        
        todo.CompletedAt.Should().BeNull();
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that attempting to update a non-existent TodoItem returns a NotFound error.
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

        var handler = new UpdateTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new UpdateTodoItemCommand(Guid.NewGuid(), "New title", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("TodoItem.NotFound");
    }

    /// <summary>
    /// Verifies that a user cannot update a TodoItem that belongs to another user,
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

        var handler = new UpdateTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new UpdateTodoItemCommand(todoId, "Hacked title", null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("TodoItem.Forbidden");
    }
}
