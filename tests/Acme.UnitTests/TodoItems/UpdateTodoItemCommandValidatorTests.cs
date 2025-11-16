using FluentAssertions;
using Acme.Application.Features.TodoItems.UpdateTodoItem;
using Xunit;

namespace Acme.UnitTests.TodoItems;

public class UpdateTodoItemCommandValidatorTests
{
    private readonly UpdateTodoItemCommandValidator validator = new();

    [Fact]
    public void ShouldFailWhenTitleProvidedButEmpty()
    {
        var cmd = new UpdateTodoItemCommand(Guid.NewGuid(), "", null, null);
        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTodoItemCommand.Title));
    }

    [Fact]
    public void ShouldPassWithNoOptionalFields()
    {
        var cmd = new UpdateTodoItemCommand(Guid.NewGuid(), null, null, null);
        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}