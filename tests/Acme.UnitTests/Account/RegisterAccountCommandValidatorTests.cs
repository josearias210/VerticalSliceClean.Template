using FluentValidation.TestHelper;
using Acme.Application.Features.Account.RegisterAccount;
using Acme.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Acme.UnitTests.Account;

public class RegisterAccountCommandValidatorTests
{
    private readonly Mock<UserManager<Domain.Entities.Account>> userManagerMock;
    private readonly RegisterAccountCommandValidator validator;

    public RegisterAccountCommandValidatorTests()
    {
        userManagerMock = MockUserManager();
        validator = new RegisterAccountCommandValidator(userManagerMock.Object);
    }

    [Fact]
    public async Task ShouldPassWhenEmailIsValidAndNotRegistered()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "newuser@example.com",
            Password = Role.User.ToString()
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((Domain.Entities.Account?)null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user")]
    public async Task ShouldFailWhenEmailIsInvalid(string email)
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = email,
            Password = Role.User.ToString()
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task ShouldFailWhenEmailAlreadyExists()
    {
        // Arrange
        var existingAccount = new Domain.Entities.Account
        {
            Email = "existing@example.com",
            UserName = "existing@example.com"
        };

        var command = new RegisterAccountCommand
        {
            Email = "existing@example.com",
            Password = Role.User.ToString()
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingAccount);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("Email.AlreadyExists")
            .WithErrorMessage("Email is already registered");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData("Manager")]
    [InlineData("Developer")]
    public async Task ShouldPassWhenRoleIsValid(string role)
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = role
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((Domain.Entities.Account?)null);

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("InvalidRole")]
    [InlineData("SuperAdmin")]
    [InlineData("Guest")]
    public async Task ShouldFailWhenRoleIsInvalid(string role)
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = role
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("Role.Invalid");
    }

    [Fact]
    public async Task ShouldFailWhenRoleIsEmpty()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("Role.Required");
    }

    private static Mock<UserManager<Domain.Entities.Account>> MockUserManager()
    {
        var store = new Mock<IUserStore<Domain.Entities.Account>>();
        return new Mock<UserManager<Domain.Entities.Account>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
