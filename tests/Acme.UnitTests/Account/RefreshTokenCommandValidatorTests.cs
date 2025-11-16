using FluentAssertions;
using FluentValidation.TestHelper;
using Acme.Application.Features.Account.RefreshToken;
using Xunit;

namespace Acme.UnitTests.Account;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator validator;

    public RefreshTokenCommandValidatorTests()
    {
        validator = new RefreshTokenCommandValidator();
    }

    [Fact]
    public void ShouldPassWhenCommandIsValid()
    {
        // Arrange
        var command = new RefreshTokenCommand();

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldAlwaysPassSinceNoValidationRulesExist()
    {
        // Arrange - RefreshTokenCommand has no properties since token comes from cookie
        var command = new RefreshTokenCommand();

        // Act
        var result = validator.TestValidate(command);

        // Assert - Should always pass as there's nothing to validate
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
