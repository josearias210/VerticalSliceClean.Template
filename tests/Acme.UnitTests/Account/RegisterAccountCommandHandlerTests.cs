using FluentAssertions;
using Acme.Application.Abstractions;
using Acme.Application.Features.Account.RegisterAccount;
using Acme.Domain.Enums;
using Acme.UnitTests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acme.UnitTests.Account;

public class RegisterAccountCommandHandlerTests
{
    private readonly Mock<UserManager<Domain.Entities.Account>> userManagerMock;
    private readonly Mock<IEmailService> emailServiceMock;
    private readonly Mock<ILogger<RegisterAccountCommandHandler>> loggerMock;
    private readonly RegisterAccountCommandHandler handler;

    public RegisterAccountCommandHandlerTests()
    {
        userManagerMock = MockUserManagerFactory.Create();
        emailServiceMock = MockServiceFactory.CreateEmailService();
        loggerMock = new Mock<ILogger<RegisterAccountCommandHandler>>();
        handler = new RegisterAccountCommandHandler(userManagerMock.Object, emailServiceMock.Object, loggerMock.Object);
    }

    /// <summary>
    /// Verifies that a new account can be created successfully with the User role
    /// and a welcome email is sent.
    /// </summary>
    [Fact]
    public async Task HandleShouldCreateAccountWithUserRoleSuccessfully()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = Role.User.ToString()
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), Role.User.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        emailServiceMock.Setup(x => x.SendWelcomeWithPasswordAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        userManagerMock.Verify(x => x.CreateAsync(
            It.Is<Domain.Entities.Account>(a => a.Email == command.Email && a.UserName == command.Email),
            It.IsAny<string>()), Times.Once);
        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), Role.User.ToString()), Times.Once);
        emailServiceMock.Verify(x => x.SendWelcomeWithPasswordAsync(command.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that a new account can be created successfully with the Admin role.
    /// </summary>
    [Fact]
    public async Task HandleShouldCreateAccountWithAdminRoleSuccessfully()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "admin@example.com",
            Password = Role.Admin.ToString()
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), Role.Admin.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        emailServiceMock.Setup(x => x.SendWelcomeWithPasswordAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), Role.Admin.ToString()), Times.Once);
    }

    /// <summary>
    /// Verifies that account creation failure (e.g., duplicate email) returns an appropriate error
    /// and prevents role assignment and email sending.
    /// </summary>
    [Fact]
    public async Task HandleShouldReturnErrorWhenCreateAccountFails()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = Role.User.ToString()
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" }));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Account.CreateFailed");
        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()), Times.Never);
        emailServiceMock.Verify(x => x.SendWelcomeWithPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that role assignment failure returns an appropriate error
    /// after the account has been successfully created.
    /// </summary>
    [Fact]
    public async Task HandleShouldReturnErrorWhenRoleAssignmentFails()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = Role.User.ToString()
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), Role.User.ToString()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidRole", Description = "Role does not exist" }));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Account.RoleAssignFailed");
    }

    /// <summary>
    /// Verifies that account registration succeeds even when the email service fails,
    /// ensuring email delivery issues don't prevent account creation.
    /// </summary>
    [Fact]
    public async Task HandleShouldNotFailWhenEmailServiceThrowsException()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = Role.User.ToString()
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), Role.User.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        emailServiceMock.Setup(x => x.SendWelcomeWithPasswordAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email service unavailable"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Registration should still succeed even if email fails
        result.IsError.Should().BeFalse();
        userManagerMock.Verify(x => x.CreateAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()), Times.Once);
        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), Role.User.ToString()), Times.Once);
    }

    /// <summary>
    /// Verifies that accounts can be created with various role types (Manager, ProductManager).
    /// Uses parameterized testing to validate multiple roles.
    /// </summary>
    [Theory]
    [InlineData("Manager")]
    [InlineData("ProductManager")]
    public async Task HandleShouldCreateAccountWithDifferentRoles(string role)
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = role
        };

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Account>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), role))
            .ReturnsAsync(IdentityResult.Success);

        emailServiceMock.Setup(x => x.SendWelcomeWithPasswordAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<Domain.Entities.Account>(), role), Times.Once);
    }
}
