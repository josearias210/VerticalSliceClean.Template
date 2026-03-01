using Acme.Domain.Constants;
using Acme.Domain.Enums;
using Acme.Domain.Extensions;
using FluentAssertions;
using Xunit;

namespace Acme.Application.Unit.Tests.Domain;

public class RoleExtensionsTests
{
    [Theory]
    [InlineData(Role.Admin, Roles.Admin)]
    [InlineData(Role.Developer, Roles.Developer)]
    [InlineData(Role.User, Roles.User)]
    [InlineData(Role.Manager, Roles.Manager)]
    public void ToRoleNameReturnsExpectedString(Role role, string expected)
    {
        var name = role.ToRoleName();

        name.Should().Be(expected);
    }

    [Theory]
    [InlineData(Roles.Admin, Role.Admin)]
    [InlineData(Roles.Developer, Role.Developer)]
    [InlineData(Roles.User, Role.User)]
    [InlineData(Roles.Manager, Role.Manager)]
    public void ToRoleParsesKnownRoles(string roleName, Role expected)
    {
        var role = roleName.ToRole();

        role.Should().Be(expected);
    }

    [Fact]
    public void ToRoleThrowsForUnknownRole()
    {
        var act = () => "Unknown".ToRole();

        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid role name: Unknown*")
            .And.ParamName.Should().Be("roleName");
    }

    [Fact]
    public void TryToRoleReturnsFalseForUnknownRole()
    {
        var result = "Unknown".TryToRole(out var role);

        result.Should().BeFalse();
        role.Should().Be(default(Role));
    }

    [Fact]
    public void TryToRoleReturnsTrueForKnownRole()
    {
        var result = Roles.Admin.TryToRole(out var role);

        result.Should().BeTrue();
        role.Should().Be(Role.Admin);
    }
}
