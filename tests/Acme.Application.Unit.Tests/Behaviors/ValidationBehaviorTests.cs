using Acme.Application.Behaviors;
using ErrorOr;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Xunit;

namespace Acme.Application.Unit.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task HandleInvokesNextWhenValidationPasses()
    {
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, ErrorOr<global::MediatR.Unit>>(validators);
        var wasCalled = false;

        var response = await behavior.Handle(new TestRequest("Value"), (ct) =>
        {
            wasCalled = true;
            return Task.FromResult<ErrorOr<global::MediatR.Unit>>(global::MediatR.Unit.Value);
        }, CancellationToken.None);

        wasCalled.Should().BeTrue();
        response.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReturnsErrorsWhenValidationFails()
    {
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, ErrorOr<global::MediatR.Unit>>(validators);

        var response = await behavior.Handle(new TestRequest(string.Empty), (ct) =>
        {
            Assert.Fail("Next delegate should not be invoked when validation fails");
            return Task.FromResult<ErrorOr<global::MediatR.Unit>>(global::MediatR.Unit.Value);
        }, CancellationToken.None);

        response.IsError.Should().BeTrue();
        response.Errors.Should().ContainSingle(e => e.Code == nameof(TestRequest.Value));
    }

    private sealed record TestRequest(string Value) : IRequest<ErrorOr<global::MediatR.Unit>>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}
