using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class LoggingMiddlewareTests
{
    private readonly RequestEvent _requestEvent = new();
    private readonly LoggingMiddleware<SampleCommand, Result> _middleware;

    public LoggingMiddlewareTests()
    {
        _middleware = new LoggingMiddleware<SampleCommand, Result>(_requestEvent);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetRequestName()
    {
        await _middleware.HandleAsync(new SampleCommand("secret"), () => Task.FromResult(Result.Success()));

        _requestEvent.Fields[RequestEventFields.RequestName].ShouldBe(nameof(SampleCommand));
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSetLevelOrErrorFields_WhenRequestSucceeds()
    {
        await _middleware.HandleAsync(new SampleCommand("secret"), () => Task.FromResult(Result.Success()));

        _requestEvent.Fields.ShouldNotContainKey(RequestEventFields.Level);
        _requestEvent.Fields.ShouldNotContainKey(RequestEventFields.ErrorCode);
        _requestEvent.Fields.ShouldNotContainKey(RequestEventFields.Exception);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetWarningAndErrorFields_WhenResultFails()
    {
        var error = Error.Validation("Sample.Invalid", "The sample was invalid");

        await _middleware.HandleAsync(new SampleCommand("secret"), () => Task.FromResult(Result.Failure(error)));

        _requestEvent.Fields[RequestEventFields.Level].ShouldBe(LogLevel.Warning);
        _requestEvent.Fields[RequestEventFields.ErrorCode].ShouldBe(error.Code);
        _requestEvent.Fields[RequestEventFields.ErrorMessage].ShouldBe(error.Message);
        _requestEvent.Fields.ShouldNotContainKey(RequestEventFields.Exception);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetErrorLevelAndExceptionAndRethrow_WhenNextThrows()
    {
        var exception = new InvalidOperationException("Handler failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _middleware.HandleAsync(new SampleCommand("secret"),
                () => throw exception));

        thrown.ShouldBeSameAs(exception);
        _requestEvent.Fields[RequestEventFields.Level].ShouldBe(LogLevel.Error);
        _requestEvent.Fields[RequestEventFields.Exception].ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task HandleAsync_ShouldNeverSet_RequestPropertyValues()
    {
        const string password = "super-secret-password";
        var error = Error.Validation("Sample.Invalid", "The sample was invalid");

        await _middleware.HandleAsync(new SampleCommand(password), () => Task.FromResult(Result.Failure(error)));

        _requestEvent.Fields.Values.ShouldNotContain(password);
    }

    [Fact]
    public async Task HandleAsync_ShouldNeverSet_RequestPropertyValues_WhenExceptionThrown()
    {
        const string password = "super-secret-password";

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _middleware.HandleAsync(new SampleCommand(password),
                () => throw new InvalidOperationException("Handler failed")));

        _requestEvent.Fields.Values.ShouldNotContain(password);
    }

    private sealed record SampleCommand(string Password) : IRequest<Result>;
}
