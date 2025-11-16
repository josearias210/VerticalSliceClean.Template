using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.Account.Login;
using Acme.Domain.Entities;
using Acme.UnitTests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acme.UnitTests.Account;

public class LoginCommandHandlerTests
{
    private readonly Mock<UserManager<Domain.Entities.Account>> userManagerMock;
    private readonly Mock<ITokenService> tokenServiceMock;
    private readonly Mock<ICookieTokenService> cookieTokenServiceMock;
    private readonly Mock<ILogger<LoginCommandHandler>> loggerMock;
    private readonly LoginCommandHandler handler;

    public LoginCommandHandlerTests()
    {
        userManagerMock = MockUserManagerFactory.Create();
        tokenServiceMock = MockServiceFactory.CreateTokenService();
        cookieTokenServiceMock = MockServiceFactory.CreateCookieTokenService();
        loggerMock = new Mock<ILogger<LoginCommandHandler>>();
        handler = new LoginCommandHandler(
            tokenServiceMock.Object,
            userManagerMock.Object,
            cookieTokenServiceMock.Object,
            loggerMock.Object);
    }

    /// <summary>
    /// Verifies that a user can login successfully when providing valid credentials.
    /// Should return a success result with the account information.
    /// </summary>
    [Fact]
    public async Task HandleShouldLoginSuccessfullyWithValidCredentials()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "ValidPassword123!"
        };

        var account = new Domain.Entities.Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = command.Email,
            UserName = command.Email,
            FullName = "Test User",
            EmailConfirmed = true
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(account);

        userManagerMock.Setup(x => x.IsLockedOutAsync(account))
            .ReturnsAsync(false);

        userManagerMock.Setup(x => x.CheckPasswordAsync(account, command.Password))
            .ReturnsAsync(true);

        userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(account))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.GetRolesAsync(account))
            .ReturnsAsync(new List<string> { "User" });

        tokenServiceMock.Setup(x => x.CreateTokensAsync(account))
            .ReturnsAsync(("access_token", "refresh_token"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(account.Id);
        result.Value.Email.Should().Be(account.Email);
        result.Value.FullName.Should().Be(account.FullName);
        result.Value.Roles.Should().Contain("User");
        result.Value.EmailConfirmed.Should().BeTrue();

        cookieTokenServiceMock.Verify(x => x.SetTokenCookies("access_token", "refresh_token"), Times.Once);
        userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(account), Times.Once);
    }

    /// <summary>
    /// Verifies that attempting to login with a non-existent email returns an invalid credentials error.
    /// Should not create tokens or set cookies when the account is not found.
    /// </summary>
    [Fact]
    public async Task HandleShouldReturnErrorWhenAccountNotFound()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "password"
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((Domain.Entities.Account?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.InvalidCredentials");
        tokenServiceMock.Verify(x => x.CreateTokensAsync(It.IsAny<Domain.Entities.Account>()), Times.Never);
        cookieTokenServiceMock.Verify(x => x.SetTokenCookies(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that attempting to login with a locked-out account returns an account locked error.
    /// Should not check the password when the account is locked out.
    /// </summary>
    [Fact]
    public async Task HandleShouldReturnErrorWhenAccountIsLockedOut()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "locked@example.com",
            Password = "password"
        };

        var account = new Domain.Entities.Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = command.Email,
            UserName = command.Email
        };

        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(account);

        userManagerMock.Setup(x => x.IsLockedOutAsync(account))
            .ReturnsAsync(true);

        userManagerMock.Setup(x => x.GetLockoutEndDateAsync(account))
            .ReturnsAsync(lockoutEnd);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.AccountLocked");
        result.FirstError.Description.Should().Contain("locked");
        userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that attempting to login with an incorrect password returns an invalid credentials error
    /// and increments the failed access count for the account.
    /// </summary>
    [Fact]
    public async Task HandleShouldReturnErrorAndIncrementFailedCountWhenPasswordIsInvalid()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        var account = new Domain.Entities.Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = command.Email,
            UserName = command.Email
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(account);

        userManagerMock.Setup(x => x.IsLockedOutAsync(account))
            .ReturnsAsync(false);

        userManagerMock.Setup(x => x.CheckPasswordAsync(account, command.Password))
            .ReturnsAsync(false);

        userManagerMock.Setup(x => x.AccessFailedAsync(account))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.GetAccessFailedCountAsync(account))
            .ReturnsAsync(1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.InvalidCredentials");
        userManagerMock.Verify(x => x.AccessFailedAsync(account), Times.Once);
        tokenServiceMock.Verify(x => x.CreateTokensAsync(It.IsAny<Domain.Entities.Account>()), Times.Never);
    }

    /// <summary>
    /// Verifies that a failure in token creation is properly handled and returned as an error
    /// even when the credentials are valid.
    /// </summary>
    [Fact]
    public async Task HandleShouldReturnErrorWhenTokenCreationFails()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "ValidPassword123!"
        };

        var account = new Domain.Entities.Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = command.Email,
            UserName = command.Email
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(account);

        userManagerMock.Setup(x => x.IsLockedOutAsync(account))
            .ReturnsAsync(false);

        userManagerMock.Setup(x => x.CheckPasswordAsync(account, command.Password))
            .ReturnsAsync(true);

        userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(account))
            .ReturnsAsync(IdentityResult.Success);

        tokenServiceMock.Setup(x => x.CreateTokensAsync(account))
            .ReturnsAsync(new List<ErrorOr.Error> { ErrorOr.Error.Failure("Token.Failed", "Token creation failed") });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Token.Failed");
        cookieTokenServiceMock.Verify(x => x.SetTokenCookies(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that when a user has multiple roles assigned, all roles are correctly included
    /// in the login response.
    /// </summary>
    [Fact]
    public async Task HandleShouldReturnMultipleRolesWhenUserHasMultipleRoles()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "admin@example.com",
            Password = "ValidPassword123!"
        };

        var account = new Domain.Entities.Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = command.Email,
            UserName = command.Email,
            EmailConfirmed = true
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(account);

        userManagerMock.Setup(x => x.IsLockedOutAsync(account))
            .ReturnsAsync(false);

        userManagerMock.Setup(x => x.CheckPasswordAsync(account, command.Password))
            .ReturnsAsync(true);

        userManagerMock.Setup(x => x.ResetAccessFailedCountAsync(account))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.GetRolesAsync(account))
            .ReturnsAsync(new List<string> { "Admin", "Manager" });

        tokenServiceMock.Setup(x => x.CreateTokensAsync(account))
            .ReturnsAsync(("access_token", "refresh_token"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Roles.Should().HaveCount(2);
        result.Value.Roles.Should().Contain("Admin");
        result.Value.Roles.Should().Contain("Manager");
    }
}
