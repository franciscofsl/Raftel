using Microsoft.Extensions.Logging;
using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class WideEventMiddlewareTests
{
    private readonly WideEvent _wideEvent;
    private readonly TestLogger _logger;

    public WideEventMiddlewareTests()
    {
        _wideEvent = new WideEvent();
        _logger = new TestLogger();
    }

    [Fact]
    public async Task HandleAsync_ShouldPopulateRequestTypeAndTimestamp()
    {
        var middleware = CreateMiddleware();
        var next = CreateNext(Result.Success());

        await middleware.HandleAsync(new TestRequest(), next);

        var properties = _wideEvent.GetProperties();
        properties["request_type"].ShouldBe("TestRequest");
        properties.ShouldContainKey("timestamp");
        properties.ShouldContainKey("trace_id");
    }

    [Fact]
    public async Task HandleAsync_WhenResultIsSuccess_ShouldSetOutcomeToSuccess()
    {
        var middleware = CreateMiddleware();
        var next = CreateNext(Result.Success());

        await middleware.HandleAsync(new TestRequest(), next);

        var properties = _wideEvent.GetProperties();
        properties["outcome"].ShouldBe("success");
        properties.ShouldContainKey("duration_ms");
    }

    [Fact]
    public async Task HandleAsync_WhenResultIsFailure_ShouldSetOutcomeToFailure()
    {
        var middleware = CreateMiddleware();
        var error = new Error("Order.NotFound", "Order not found");
        var next = CreateNext(Result.Failure(error));

        await middleware.HandleAsync(new TestRequest(), next);

        var properties = _wideEvent.GetProperties();
        properties["outcome"].ShouldBe("failure");
        properties["error_code"].ShouldBe("Order.NotFound");
        properties["error_message"].ShouldBe("Order not found");
    }

    [Fact]
    public async Task HandleAsync_WhenExceptionThrown_ShouldCaptureErrorDetails()
    {
        var middleware = CreateMiddleware();
        RequestHandlerDelegate<Result> next = () => throw new InvalidOperationException("Test error");

        await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.HandleAsync(new TestRequest(), next));

        var properties = _wideEvent.GetProperties();
        properties["outcome"].ShouldBe("error");
        properties["error_type"].ShouldBe("InvalidOperationException");
        properties["error_message"].ShouldBe("Test error");
    }

    [Fact]
    public async Task HandleAsync_ShouldCaptureDurationMs()
    {
        var middleware = CreateMiddleware();
        var next = CreateNext(Result.Success());

        await middleware.HandleAsync(new TestRequest(), next);

        var properties = _wideEvent.GetProperties();
        properties.ShouldContainKey("duration_ms");
        ((long)properties["duration_ms"]).ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task HandleAsync_ShouldAllowEnrichmentFromHandler()
    {
        var middleware = CreateMiddleware();
        RequestHandlerDelegate<Result> next = () =>
        {
            _wideEvent.Add("user_id", "user_789");
            _wideEvent.Add("order_id", "order_abc");
            return Task.FromResult(Result.Success());
        };

        await middleware.HandleAsync(new TestRequest(), next);

        var properties = _wideEvent.GetProperties();
        properties["user_id"].ShouldBe("user_789");
        properties["order_id"].ShouldBe("order_abc");
    }

    [Fact]
    public async Task HandleAsync_ShouldEmitLogEntry()
    {
        var middleware = CreateMiddleware();
        var next = CreateNext(Result.Success());

        await middleware.HandleAsync(new TestRequest(), next);

        _logger.LogCount.ShouldBe(1);
    }

    private WideEventMiddleware<TestRequest, Result> CreateMiddleware()
    {
        return new WideEventMiddleware<TestRequest, Result>(_wideEvent, _logger);
    }

    private static RequestHandlerDelegate<Result> CreateNext(Result result)
    {
        return () => Task.FromResult(result);
    }

    internal class TestRequest : IRequest<Result>
    {
    }

    private sealed class TestLogger : ILogger<WideEventMiddleware<TestRequest, Result>>
    {
        public int LogCount { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new NoopDisposable();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            LogCount++;
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
