using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.Account.GetProfile;
using MockQueryable.Moq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acme.UnitTests.Account;

public class GetProfileQueryHandlerTests
{
    private readonly Mock<IUserIdentityService> userIdentityServiceMock;
    private readonly Mock<ILogger<GetProfileQueryHandler>> loggerMock;

    public GetProfileQueryHandlerTests()
    {
        userIdentityServiceMock = new Mock<IUserIdentityService>();
        loggerMock = new Mock<ILogger<GetProfileQueryHandler>>();
    }

    [Fact]
    public async Task HandleShouldReturnProfileWhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var account = new Domain.Entities.Account
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user@example.com",
            FullName = "Test User"
        };

        var handler = CreateHandler(new List<Domain.Entities.Account> { account }, userId, account.Email);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(userId);
        result.Value.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task HandleShouldReturnUnauthorizedWhenUserIdIsNull()
    {
        // Arrange
        var handler = CreateHandler(new List<Domain.Entities.Account>(), null, "user@example.com");
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task HandleShouldReturnUnauthorizedWhenEmailIsNull()
    {
        // Arrange
        var handler = CreateHandler(new List<Domain.Entities.Account>(), Guid.NewGuid().ToString(), null);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task HandleShouldReturnNotFoundWhenAccountDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var handler = CreateHandler(new List<Domain.Entities.Account>(), userId, "nonexistent@example.com");
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Account.NotFound");
    }

    [Fact]
    public async Task HandleShouldReturnCorrectProfileForDifferentUsers()
    {
        // Arrange
        var user1Id = Guid.NewGuid().ToString();
        var user2Id = Guid.NewGuid().ToString();

        var account1 = new Domain.Entities.Account
        {
            Id = user1Id,
            Email = "user1@example.com",
            UserName = "user1@example.com"
        };

        var account2 = new Domain.Entities.Account
        {
            Id = user2Id,
            Email = "user2@example.com",
            UserName = "user2@example.com"
        };

        var handler = CreateHandler(new List<Domain.Entities.Account> { account1, account2 }, user1Id, "user1@example.com");
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(user1Id);
        result.Value.Email.Should().Be("user1@example.com");
    }

    private GetProfileQueryHandler CreateHandler(List<Domain.Entities.Account> accounts, string? userId, string? email)
    {
        var mockDbSet = accounts.AsQueryable().BuildMockDbSet();

        var dbContextMock = new Mock<IApplicationDbContext>();
        dbContextMock.Setup(x => x.Accounts).Returns(mockDbSet.Object);

        userIdentityServiceMock.Setup(x => x.GetUserId()).Returns(userId);
        userIdentityServiceMock.Setup(x => x.GetEmail()).Returns(email);

        return new GetProfileQueryHandler(userIdentityServiceMock.Object, dbContextMock.Object, loggerMock.Object);
    }
}
