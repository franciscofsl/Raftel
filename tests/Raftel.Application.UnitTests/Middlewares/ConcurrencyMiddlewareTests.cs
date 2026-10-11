using Raftel.Application.Abstractions.Concurrency;
using Raftel.Application.Commands;
using Raftel.Application.Exceptions;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class ConcurrencyMiddlewareTests
{
    private readonly ConcurrencyMiddleware<SampleCommand> _middleware = new();

    [Fact]
    public async Task HandleAsync_WhenNextThrowsConflict_ShouldReturnConflictFailure()
    {
        var result = await _middleware.HandleAsync(new SampleCommand(),
            _ => throw new ConcurrencyConflictException(["Tenant"]), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Concurrency.Conflict");
        result.Error.Code.ShouldBe(ConcurrencyErrors.ConflictCode);
        result.Error.Message.ShouldContain("Tenant");
    }

    [Fact]
    public async Task HandleAsync_WhenNextSucceeds_ShouldReturnResultUnchanged()
    {
        var result = await _middleware.HandleAsync(new SampleCommand(), _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenNextReturnsFailure_ShouldReturnItUnchanged()
    {
        var error = Error.Validation("Sample.Invalid", "invalid");

        var result = await _middleware.HandleAsync(new SampleCommand(), _ => Task.FromResult(Result.Failure(error)),
            CancellationToken.None);

        result.Error.ShouldBe(error);
    }

    [Fact]
    public async Task HandleAsync_WhenNextThrowsAnotherException_ShouldPropagateIt()
    {
        var exception = new InvalidOperationException("boom");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _middleware.HandleAsync(new SampleCommand(), _ => throw exception, CancellationToken.None));

        thrown.ShouldBeSameAs(exception);
    }

    private sealed record SampleCommand : ICommand;
}

public class ConcurrencyMiddlewareOfTResultTests
{
    private readonly ConcurrencyMiddleware<SampleCommand, string> _middleware = new();

    [Fact]
    public async Task HandleAsync_WhenNextThrowsConflict_ShouldReturnTypedConflictFailure()
    {
        var result = await _middleware.HandleAsync(new SampleCommand(),
            _ => throw new ConcurrencyConflictException(["User"]), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Concurrency.Conflict");
    }

    [Fact]
    public async Task HandleAsync_WhenNextSucceeds_ShouldReturnTypedValueUnchanged()
    {
        var result = await _middleware.HandleAsync(new SampleCommand(),
            _ => Task.FromResult(Result.Success("value")), CancellationToken.None);

        result.Value.ShouldBe("value");
    }

    [Fact]
    public async Task HandleAsync_WhenNextThrowsAnotherException_ShouldPropagateIt()
    {
        var exception = new InvalidOperationException("boom");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _middleware.HandleAsync(new SampleCommand(), _ => throw exception, CancellationToken.None));

        thrown.ShouldBeSameAs(exception);
    }

    private sealed record SampleCommand : ICommand<string>;
}
