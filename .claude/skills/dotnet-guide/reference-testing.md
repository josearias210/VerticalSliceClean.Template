# Testing Patterns Reference

## Framework Stack

- **xUnit 2.9.3** — test runner
- **FluentAssertions 8.8.0** — readable assertions with `.Should()`
- **Moq 4.20.72** — mock objects
- **MockQueryable.Moq** — EF Core queryable mocking

## Test File Organization

Mirror the source structure exactly:

```
tests/Acme.Application.Unit.Tests/
├── Features/
│   └── Account/
│       └── RegisterAccount/
│           └── RegisterAccountCommandValidatorTests.cs
├── Behaviors/
│   └── ValidationBehaviorTests.cs
└── Domain/
    └── RoleExtensionsTests.cs
```

## Validator Tests

```csharp
namespace Acme.Application.Unit.Tests.Features.Account.RegisterAccount;

public class RegisterAccountCommandValidatorTests
{
    private readonly RegisterAccountCommandValidator _validator = new();

    [Fact]
    public void ValidateReturnsErrorsWhenMandatoryFieldsAreMissing()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            FirstName = string.Empty,
            Email = string.Empty,
            Role = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.FirstNameEmpty);
        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.EmailEmpty);
        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.RoleEmpty);
    }

    [Fact]
    public void ValidateReturnsNoErrorsWhenAllFieldsAreValid()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            FirstName = "John",
            Email = "john@example.com",
            Role = Role.Admin
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateReturnsErrorWhenRoleIsDeveloper()
    {
        // Arrange
        var command = new RegisterAccountCommand
        {
            FirstName = "John",
            Email = "john@example.com",
            Role = Role.Developer
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().Contain(e => e.ErrorCode == ErrorCodes.Account.DeveloperRoleNotAllowed);
    }
}
```

## Pipeline Behavior Tests

```csharp
public class ValidationBehaviorTests
{
    [Fact]
    public async Task HandleInvokesNextWhenValidationPasses()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, ErrorOr<Unit>>(validators);
        var wasCalled = false;

        // Act
        var response = await behavior.Handle(
            new TestRequest("ValidValue"),
            (ct) =>
            {
                wasCalled = true;
                return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
            },
            CancellationToken.None);

        // Assert
        wasCalled.Should().BeTrue();
        response.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReturnsErrorsWhenValidationFails()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, ErrorOr<Unit>>(validators);

        // Act
        var response = await behavior.Handle(
            new TestRequest(string.Empty),
            (ct) =>
            {
                Assert.Fail("Next delegate should not be invoked when validation fails");
                return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
            },
            CancellationToken.None);

        // Assert
        response.IsError.Should().BeTrue();
        response.Errors.Should().ContainSingle(e => e.Code == nameof(TestRequest.Value));
    }

    [Fact]
    public async Task HandleInvokesNextWhenNoValidatorsRegistered()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, ErrorOr<Unit>>(validators);
        var wasCalled = false;

        // Act
        var response = await behavior.Handle(
            new TestRequest(string.Empty),
            (ct) =>
            {
                wasCalled = true;
                return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
            },
            CancellationToken.None);

        // Assert
        wasCalled.Should().BeTrue();
        response.IsError.Should().BeFalse();
    }

    // Test helpers — inner types
    private sealed record TestRequest(string Value) : IRequest<ErrorOr<Unit>>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}
```

## Domain Extension Tests

```csharp
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
        var act = () => "UnknownRole".ToRole();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToRoleNameThrowsForInvalidEnum()
    {
        var act = () => ((Role)999).ToRoleName();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

## Testing Conventions Summary

| Convention | Rule |
|-----------|------|
| **Class name** | `{TargetClass}Tests` |
| **Method name** | `{MethodUnderTest}{Scenario}{ExpectedResult}` |
| **Single case** | `[Fact]` attribute |
| **Multiple cases** | `[Theory]` + `[InlineData]` |
| **Assertions** | FluentAssertions (`.Should()`) — never raw `Assert` |
| **Error validation** | Assert against error codes, never message strings |
| **Arrange-Act-Assert** | Always use this pattern, even for simple tests |
| **Test helpers** | Inner types (`private sealed record/class`) within test class |
| **Field setup** | Direct instantiation (`new()`) in field declaration, not constructor |
| **Async tests** | Return `async Task`, use `CancellationToken.None` |
