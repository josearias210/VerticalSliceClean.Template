using FluentValidation.TestHelper;
using Acme.Application.Features.Account.Login;
using Xunit;

namespace Acme.UnitTests.Account;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator validator;

    public LoginCommandValidatorTests()
    {
        validator = new LoginCommandValidator();
    }

    [Fact]
    public void ShouldPassWhenEmailAndPasswordAreValid()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "ValidPassword123!"
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user")]
    public void ShouldFailWhenEmailIsInvalid(string email)
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = email,
            Password = "password"
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFailWhenPasswordIsEmpty(string? password)
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = password!
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldPassWhenPasswordIsSimple()
    {
        // Arrange - For login, we don't validate password complexity, only that it's not empty
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "simple"
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
