using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Acme.UnitTests.Mocks;

/// <summary>
/// Factory for creating mock ApplicationDbContext instances for testing.
/// </summary>
public static class MockDbContextFactory
{
    /// <summary>
    /// Creates a mock ApplicationDbContext configured with a TodoItems DbSet.
    /// </summary>
    /// <param name="mockTodoItemsDbSet">The mock DbSet of TodoItems to use.</param>
    /// <returns>A configured Mock of IApplicationDbContext.</returns>
    public static Mock<IApplicationDbContext> CreateWithTodoItems(Mock<DbSet<TodoItem>> mockTodoItemsDbSet)
    {
        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.TodoItems).Returns(mockTodoItemsDbSet.Object);
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mockDbContext;
    }

    /// <summary>
    /// Creates a basic mock ApplicationDbContext.
    /// </summary>
    /// <returns>A configured Mock of IApplicationDbContext.</returns>
    public static Mock<IApplicationDbContext> Create()
    {
        var mockDbContext = new Mock<IApplicationDbContext>();
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mockDbContext;
    }
}
