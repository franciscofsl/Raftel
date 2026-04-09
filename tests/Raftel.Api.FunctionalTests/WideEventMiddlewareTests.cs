using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Raftel.Api.Server.Middlewares;
using Raftel.Application.Abstractions;
using Shouldly;

namespace Raftel.Api.FunctionalTests;

public class WideEventMiddlewareTests
{
    private readonly ILogger<WideEventMiddleware> _logger;

    public WideEventMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<WideEventMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPopulateRequestProperties()
    {
        var wideEvent = new WideEvent();
        var context = CreateHttpContext(wideEvent, "GET", "/api/test");

        var middleware = new WideEventMiddleware(_ => Task.CompletedTask, _logger);

        await middleware.InvokeAsync(context);

        var properties = wideEvent.GetProperties();
        properties["method"].ShouldBe("GET");
        properties["path"].ShouldBe("/api/test");
        properties.ShouldContainKey("request_id");
        properties.ShouldContainKey("trace_id");
        properties.ShouldContainKey("timestamp");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCaptureStatusCodeAndDuration()
    {
        var wideEvent = new WideEvent();
        var context = CreateHttpContext(wideEvent, "POST", "/api/checkout");

        var middleware = new WideEventMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 201;
            return Task.CompletedTask;
        }, _logger);

        await middleware.InvokeAsync(context);

        var properties = wideEvent.GetProperties();
        properties["status_code"].ShouldBe(201);
        properties["outcome"].ShouldBe("success");
        properties.ShouldContainKey("duration_ms");
    }

    [Fact]
    public async Task InvokeAsync_WhenServerError_ShouldSetOutcomeToError()
    {
        var wideEvent = new WideEvent();
        var context = CreateHttpContext(wideEvent, "GET", "/api/fail");

        var middleware = new WideEventMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 500;
            return Task.CompletedTask;
        }, _logger);

        await middleware.InvokeAsync(context);

        var properties = wideEvent.GetProperties();
        properties["status_code"].ShouldBe(500);
        properties["outcome"].ShouldBe("error");
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldCaptureErrorDetails()
    {
        var wideEvent = new WideEvent();
        var context = CreateHttpContext(wideEvent, "GET", "/api/throw");

        var middleware = new WideEventMiddleware(
            _ => throw new InvalidOperationException("Test error"), _logger);

        await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        var properties = wideEvent.GetProperties();
        properties["error_type"].ShouldBe("InvalidOperationException");
        properties["error_message"].ShouldBe("Test error");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowEnrichmentFromDownstreamMiddleware()
    {
        var wideEvent = new WideEvent();
        var context = CreateHttpContext(wideEvent, "POST", "/api/order");

        var middleware = new WideEventMiddleware(ctx =>
        {
            var evt = ctx.RequestServices.GetRequiredService<IWideEvent>();
            evt.Add("user_id", "user_789");
            evt.Add("order_id", "order_abc");
            return Task.CompletedTask;
        }, _logger);

        await middleware.InvokeAsync(context);

        var properties = wideEvent.GetProperties();
        properties["user_id"].ShouldBe("user_789");
        properties["order_id"].ShouldBe("order_abc");
    }

    [Fact]
    public async Task InvokeAsync_ShouldEmitLogEntry()
    {
        var wideEvent = new WideEvent();
        var context = CreateHttpContext(wideEvent, "GET", "/api/test");

        var middleware = new WideEventMiddleware(_ => Task.CompletedTask, _logger);

        await middleware.InvokeAsync(context);

        _logger.ReceivedWithAnyArgs(1).Log(
            default,
            default,
            default,
            default,
            default);
    }

    private static DefaultHttpContext CreateHttpContext(WideEvent wideEvent, string method, string path)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWideEvent>(wideEvent);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        context.Request.Method = method;
        context.Request.Path = path;

        return context;
    }
}
