using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.Account.RefreshToken;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Acme.UnitTests.Account;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<ITokenService> tokenServiceMock;
    private readonly Mock<UserManager<Domain.Entities.Account>> userManagerMock;
    private readonly Mock<ICookieTokenService> cookieTokenServiceMock;
    private readonly Mock<IHttpContextAccessor> httpContextAccessorMock;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> loggerMock;
    private readonly RefreshTokenCommandHandler handler;

    public RefreshTokenCommandHandlerTests()
    {
        tokenServiceMock = new Mock<ITokenService>();
        userManagerMock = MockUserManager();
        cookieTokenServiceMock = new Mock<ICookieTokenService>();
        httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        loggerMock = new Mock<ILogger<RefreshTokenCommandHandler>>();

        handler = new RefreshTokenCommandHandler(
            tokenServiceMock.Object,
            userManagerMock.Object,
            cookieTokenServiceMock.Object,
            httpContextAccessorMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task HandleShouldRefreshTokenSuccessfullyWithValidRefreshToken()
    {
        // Arrange
        var command = new RefreshTokenCommand();
        var userId = Guid.NewGuid().ToString();
        var refreshToken = "valid_refresh_token";
        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";

        var account = new Domain.Entities.Account
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user@example.com",
            FullName = "Test User",
            EmailConfirmed = true
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId)
        }));

        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        cookieTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(refreshToken);
        tokenServiceMock.Setup(x => x.RefreshAsync(refreshToken, It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((newAccessToken, newRefreshToken));
        userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(account);
        userManagerMock.Setup(x => x.GetRolesAsync(account))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(userId);
        result.Value.Email.Should().Be("user@example.com");
        result.Value.FullName.Should().Be("Test User");
        result.Value.Roles.Should().Contain("User");
        result.Value.EmailConfirmed.Should().BeTrue();

        cookieTokenServiceMock.Verify(x => x.SetTokenCookies(newAccessToken, newRefreshToken), Times.Once);
    }

    [Fact]
    public async Task HandleShouldReturnErrorWhenRefreshTokenNotInCookie()
    {
        // Arrange
        var command = new RefreshTokenCommand();
        cookieTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns((string?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.MissingRefreshToken");
        tokenServiceMock.Verify(x => x.RefreshAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task HandleShouldReturnErrorWhenTokenServiceFails()
    {
        // Arrange
        var command = new RefreshTokenCommand();
        var refreshToken = "invalid_refresh_token";

        cookieTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(refreshToken);
        tokenServiceMock.Setup(x => x.RefreshAsync(refreshToken, It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<ErrorOr.Error> 
            { 
                ErrorOr.Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token") 
            });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.InvalidRefreshToken");
        cookieTokenServiceMock.Verify(x => x.SetTokenCookies(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleShouldReturnErrorWhenUserNotFoundAfterRefresh()
    {
        // Arrange
        var command = new RefreshTokenCommand();
        var userId = Guid.NewGuid().ToString();
        var refreshToken = "valid_refresh_token";

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId)
        }));

        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        cookieTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(refreshToken);
        tokenServiceMock.Setup(x => x.RefreshAsync(refreshToken, It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(("new_access_token", "new_refresh_token"));
        userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((Domain.Entities.Account?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.UserNotFound");
    }

    [Fact]
    public async Task HandleShouldCaptureIpAndUserAgent()
    {
        // Arrange
        var command = new RefreshTokenCommand();
        var userId = Guid.NewGuid().ToString();
        var refreshToken = "valid_refresh_token";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        var account = new Domain.Entities.Account
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user@example.com"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ipAddress);
        httpContext.Request.Headers["User-Agent"] = userAgent;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId)
        }));

        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        cookieTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(refreshToken);
        tokenServiceMock.Setup(x => x.RefreshAsync(refreshToken, ipAddress, userAgent))
            .ReturnsAsync(("new_access_token", "new_refresh_token"));
        userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(account);
        userManagerMock.Setup(x => x.GetRolesAsync(account))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        tokenServiceMock.Verify(x => x.RefreshAsync(refreshToken, ipAddress, userAgent), Times.Once);
    }

    [Fact]
    public async Task HandleShouldReturnMultipleRolesWhenUserHasMultipleRoles()
    {
        // Arrange
        var command = new RefreshTokenCommand();
        var userId = Guid.NewGuid().ToString();
        var refreshToken = "valid_refresh_token";

        var account = new Domain.Entities.Account
        {
            Id = userId,
            Email = "admin@example.com",
            UserName = "admin@example.com"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId)
        }));

        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        cookieTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(refreshToken);
        tokenServiceMock.Setup(x => x.RefreshAsync(refreshToken, It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(("new_access_token", "new_refresh_token"));
        userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(account);
        userManagerMock.Setup(x => x.GetRolesAsync(account))
            .ReturnsAsync(new List<string> { "Admin", "Manager" });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Roles.Should().HaveCount(2);
        result.Value.Roles.Should().Contain("Admin");
        result.Value.Roles.Should().Contain("Manager");
    }

    private static Mock<UserManager<Domain.Entities.Account>> MockUserManager()
    {
        var store = new Mock<IUserStore<Domain.Entities.Account>>();
        return new Mock<UserManager<Domain.Entities.Account>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
