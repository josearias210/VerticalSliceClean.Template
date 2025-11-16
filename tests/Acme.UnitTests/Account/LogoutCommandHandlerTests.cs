using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.Account.Logout;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acme.UnitTests.Account;

public class LogoutCommandHandlerTests
{
    private readonly Mock<ICookieTokenService> cookieTokenServiceMock;
    private readonly Mock<ILogger<LogoutCommandHandler>> loggerMock;
    private readonly LogoutCommandHandler handler;

    public LogoutCommandHandlerTests()
    {
        cookieTokenServiceMock = new Mock<ICookieTokenService>();
        loggerMock = new Mock<ILogger<LogoutCommandHandler>>();
        handler = new LogoutCommandHandler(cookieTokenServiceMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task HandleShouldClearCookiesAndReturnTrue()
    {
        // Arrange
        var command = new LogoutCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        cookieTokenServiceMock.Verify(x => x.ClearTokenCookies(), Times.Once);
    }

    [Fact]
    public async Task HandleShouldAlwaysSucceed()
    {
        // Arrange
        var command = new LogoutCommand();

        // Setup to verify it doesn't throw even if ClearTokenCookies does nothing
        cookieTokenServiceMock.Setup(x => x.ClearTokenCookies())
            .Verifiable();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        cookieTokenServiceMock.Verify();
    }
}
