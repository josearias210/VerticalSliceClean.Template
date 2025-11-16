using FluentAssertions;
using Acme.Application.Features.TodoItems.CreateTodoItem;
using Xunit;

namespace Acme.UnitTests.TodoItems;

public class CreateTodoItemCommandValidatorTests
{
    private readonly CreateTodoItemCommandValidator validator = new();

    [Fact]
    public void ShouldFailWhenTitleIsEmpty()
    {
        var cmd = new CreateTodoItemCommand(string.Empty, "desc");
        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTodoItemCommand.Title));
    }

    [Fact]
    public void ShouldPassWithValidData()
    {
        var cmd = new CreateTodoItemCommand("Buy milk", "2 liters");
        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}