using Acme.Application.Common;
using Acme.Application.Features.Account.RegisterAccount;
using Acme.Domain.Enums;
using FluentAssertions;

namespace Acme.Application.Tests.Features.Account.RegisterAccount;

public class RegisterAccountCommandValidatorTests
{
    private readonly RegisterAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_ReturnsErrors_WhenMandatoryFieldsAreMissing()
    {
        var command = new RegisterAccountCommand
        {
            FirstName = string.Empty,
            Email = string.Empty,
            Role = 0
        };

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.FirstNameEmpty);
        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.EmailEmpty);
        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.RoleEmpty);
    }

    [Fact]
    public void Validate_ReturnsRoleInvalid_WhenRoleIsOutOfRange()
    {
        var command = new RegisterAccountCommand
        {
            FirstName = "John",
            Email = "john@example.com",
            Role = (Role)99
        };

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.RoleInvalid);
    }

    [Fact]
    public void Validate_BlocksDeveloperRole()
    {
        var command = new RegisterAccountCommand
        {
            FirstName = "Jane",
            Email = "jane@example.com",
            Role = Role.Developer
        };

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.DeveloperRoleNotAllowed);
    }
}
