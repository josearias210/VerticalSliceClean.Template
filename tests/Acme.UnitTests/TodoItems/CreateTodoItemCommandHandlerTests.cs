using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.TodoItems.CreateTodoItem;
using Acme.Domain.Entities;
using Acme.UnitTests.Mocks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Acme.UnitTests.TodoItems;

public class CreateTodoItemCommandHandlerTests
{
    private readonly string testUserId = Guid.NewGuid().ToString();

    /// <summary>
    /// Verifies that a new TodoItem with title and description is created successfully
    /// and associated with the current user.
    /// </summary>
    [Fact]
    public async Task ShouldCreateTodoItemSuccessfully()
    {
        // Arrange
        var mockDbContext = new Mock<IApplicationDbContext>();
        var mockTodoItemsDbSet = new Mock<DbSet<TodoItem>>();
        
        TodoItem? capturedTodoItem = null;
        mockTodoItemsDbSet.Setup(m => m.Add(It.IsAny<TodoItem>()))
            .Callback<TodoItem>(item => capturedTodoItem = item);
        
        mockDbContext.Setup(x => x.TodoItems).Returns(mockTodoItemsDbSet.Object);
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new CreateTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new CreateTodoItemCommand("Buy groceries", "Milk, bread, eggs");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Title.Should().Be("Buy groceries");
        result.Value.Description.Should().Be("Milk, bread, eggs");
        result.Value.IsCompleted.Should().BeFalse();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        capturedTodoItem.Should().NotBeNull();
        capturedTodoItem!.CreatedByAccountId.Should().Be(testUserId);
        capturedTodoItem.Title.Should().Be("Buy groceries");
        
        mockTodoItemsDbSet.Verify(m => m.Add(It.IsAny<TodoItem>()), Times.Once);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that a TodoItem can be created with only a title (null description)
    /// and the description field is correctly handled as optional.
    /// </summary>
    [Fact]
    public async Task ShouldCreateTodoItemWithoutDescription()
    {
        // Arrange
        var mockDbContext = new Mock<IApplicationDbContext>();
        var mockTodoItemsDbSet = new Mock<DbSet<TodoItem>>();
        
        TodoItem? capturedTodoItem = null;
        mockTodoItemsDbSet.Setup(m => m.Add(It.IsAny<TodoItem>()))
            .Callback<TodoItem>(item => capturedTodoItem = item);
        
        mockDbContext.Setup(x => x.TodoItems).Returns(mockTodoItemsDbSet.Object);
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var userIdentityService = new Mock<IUserIdentityService>();
        userIdentityService.Setup(x => x.GetUserId()).Returns(testUserId);

        var handler = new CreateTodoItemCommandHandler(mockDbContext.Object, userIdentityService.Object);
        var command = new CreateTodoItemCommand("Simple task", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Description.Should().BeNull();
        
        capturedTodoItem.Should().NotBeNull();
        capturedTodoItem!.Description.Should().BeNull();
        
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
