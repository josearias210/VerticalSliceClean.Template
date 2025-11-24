using Acme.Application.Behaviors;
using ErrorOr;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace Acme.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_InvokesNext_WhenValidationPasses()
    {
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, ErrorOr<Unit>>(validators);
        var wasCalled = false;

        var response = await behavior.Handle(new TestRequest("Value"), () =>
        {
            wasCalled = true;
            return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
        }, CancellationToken.None);

        wasCalled.Should().BeTrue();
        response.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsErrors_WhenValidationFails()
    {
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, ErrorOr<Unit>>(validators);

        var response = await behavior.Handle(new TestRequest(string.Empty), () =>
        {
            Assert.Fail("Next delegate should not be invoked when validation fails");
            return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
        }, CancellationToken.None);

        response.IsError.Should().BeTrue();
        response.Errors.Should().ContainSingle(e => e.Code == nameof(TestRequest.Value));
    }

    private sealed record TestRequest(string Value) : IRequest<ErrorOr<Unit>>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}
