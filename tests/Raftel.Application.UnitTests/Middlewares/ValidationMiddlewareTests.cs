using Raftel.Application.Abstractions;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Validators;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class ValidationMiddlewareTests
{
    [Fact]
    public async Task HandleAsync_WhenNoValidatorsRegistered_ShouldInvokeNextAndReturnItsResult()
    {
        var middleware = new ValidationMiddleware<TestCommand, Result>([]);
        var nextInvoked = false;
        RequestHandlerDelegate<Result> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult(Result.Success());
        };

        var result = await middleware.HandleAsync(new TestCommand("Luffy"), next, CancellationToken.None);

        nextInvoked.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenValidationPasses_ShouldInvokeNextAndReturnItsResult()
    {
        var middleware = new ValidationMiddleware<TestCommand, Result>([new NameRequiredValidator()]);
        RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Success());

        var result = await middleware.HandleAsync(new TestCommand("Luffy"), next, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ShouldReturnFailedResultWithoutInvokingNext()
    {
        var middleware = new ValidationMiddleware<TestCommand, Result>([new NameRequiredValidator()]);
        var nextInvoked = false;
        RequestHandlerDelegate<Result> next = _ =>
        {
            nextInvoked = true;
            return Task.FromResult(Result.Success());
        };

        var result = await middleware.HandleAsync(new TestCommand(""), next, CancellationToken.None);

        nextInvoked.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenValidationFails_ErrorShouldBeValidationErrorAggregatingEveryFailedRule()
    {
        var middleware = new ValidationMiddleware<TestCommand, Result>(
        [
            new NameRequiredValidator(),
            new BountyMinimumValidator()
        ]);
        RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Success());

        var result = await middleware.HandleAsync(new TestCommand(""), next, CancellationToken.None);

        var validationError = result.Error.ShouldBeOfType<ValidationError>();
        validationError.Errors.Count.ShouldBe(2);
        validationError.Errors.ShouldContain(e => e.Code == "Name.Required");
        validationError.Errors.ShouldContain(e => e.Code == "Bounty.TooLow");
    }

    [Fact]
    public async Task HandleAsync_ForResultOfT_WhenValidationFails_ShouldReturnFailedResultOfT()
    {
        var middleware = new ValidationMiddleware<TestCommandWithResult, Result<string>>([new EchoRequiredValidator()]);
        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(Result.Success("unused"));

        var result = await middleware.HandleAsync(new TestCommandWithResult(""), next, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
    }

    private sealed record TestCommand(string Name) : IRequest<Result>;

    private sealed record TestCommandWithResult(string Echo) : IRequest<Result<string>>;

    private sealed class NameRequiredValidator : Validator<TestCommand>
    {
        public NameRequiredValidator() =>
            EnsureThat(c => !string.IsNullOrWhiteSpace(c.Name), Error.Validation("Name.Required", "Name is required."));
    }

    private sealed class BountyMinimumValidator : Validator<TestCommand>
    {
        public BountyMinimumValidator() =>
            EnsureThat(_ => false, Error.Validation("Bounty.TooLow", "Bounty is too low."));
    }

    private sealed class EchoRequiredValidator : Validator<TestCommandWithResult>
    {
        public EchoRequiredValidator() =>
            EnsureThat(c => !string.IsNullOrWhiteSpace(c.Echo), Error.Validation("Echo.Required", "Echo is required."));
    }
}
