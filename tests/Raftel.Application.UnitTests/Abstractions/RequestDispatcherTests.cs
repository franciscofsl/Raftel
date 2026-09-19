using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Abstractions;

public sealed class RequestDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WithAlreadyCancelledToken_ShouldPropagateItToHandler()
    {
        var handler = Substitute.For<IRequestHandler<TestCommand, Result>>();
        handler
            .HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var services = new ServiceCollection();
        services.AddSingleton(handler);
        var provider = services.BuildServiceProvider();
        var dispatcher = new RequestDispatcher(provider);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await dispatcher.DispatchAsync<TestCommand, Result>(new TestCommand(), cts.Token);

        await handler.Received(1).HandleAsync(
            Arg.Any<TestCommand>(),
            Arg.Is<CancellationToken>(token => token.IsCancellationRequested));
    }

    [Fact]
    public async Task DispatchAsync_Command_ShouldExecuteMiddlewaresInExpectedOrder()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISpy, Spy>();
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RequestDispatcherTests).Assembly);
            cfg.AddGlobalMiddleware(typeof(GlobalMiddleware1<,>));
            cfg.AddGlobalMiddleware(typeof(GlobalMiddleware2<,>));
            cfg.AddCommandMiddleware(typeof(CommandMiddleware1<>));
        });

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        await dispatcher.DispatchAsync(new TestCommand());

        var spy = provider.GetRequiredService<ISpy>();
        var interceptedMessage = spy.InterceptedMessages();

        interceptedMessage.ShouldBe(new[]
            { "Hi Global 1", "Hi Global 2", "Hi Command 1", "Handler", "By Command 1", "By Global 2", "By Global 1" });
    }

    [Fact]
    public async Task DispatchAsync_Query_ShouldExecuteMiddlewaresInExpectedOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RequestDispatcherTests).Assembly);
            cfg.AddGlobalMiddleware(typeof(GlobalMiddleware1<,>));
            cfg.AddGlobalMiddleware(typeof(GlobalMiddleware2<,>));
            cfg.AddQueryMiddleware(typeof(QueryMiddleware1<,>));
            cfg.AddQueryMiddleware(typeof(QueryMiddleware2<,>));
        });

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IQueryDispatcher>();

        var result = await dispatcher.DispatchAsync<TestQuery, string>(new TestQuery());

        var spy = provider.GetRequiredService<ISpy>();
        var interceptedMessage = spy.InterceptedMessages();
        interceptedMessage.ShouldBe(new[]
        {
            "Hi Global 1", "Hi Global 2", "Hi Query 1", "Hi Query 2", "Query", "By Query 2", "By Query 1",
            "By Global 2", "By Global 1"
        });
    }

    [Fact]
    public async Task DispatchAsync_CommandWithResult_ShouldExecuteGlobalMiddlewaresAndReturnValue()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RequestDispatcherTests).Assembly);
            cfg.AddGlobalMiddleware(typeof(GlobalMiddleware1<,>));
            cfg.AddGlobalMiddleware(typeof(GlobalMiddleware2<,>));
        });

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.DispatchAsync<TestCommandWithResult, string>(
            new TestCommandWithResult("Raftel"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Raftel");

        var spy = provider.GetRequiredService<ISpy>();
        var interceptedMessage = spy.InterceptedMessages();
        interceptedMessage.ShouldBe(new[]
            { "Hi Global 1", "Hi Global 2", "HandlerWithResult", "By Global 2", "By Global 1" });
    }

    [Fact]
    public async Task DispatchAsync_CommandWithResult_ShouldExecuteCommandMiddlewaresInExpectedOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISpy, Spy>();
        services.AddRaftelApplication(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RequestDispatcherTests).Assembly);
            cfg.AddGlobalMiddleware(typeof(GlobalMiddleware1<,>));
            cfg.AddCommandMiddleware(typeof(CommandWithResultMiddleware1<,>));
        });

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.DispatchAsync<TestCommandWithResult, string>(
            new TestCommandWithResult("Raftel"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Raftel");

        var spy = provider.GetRequiredService<ISpy>();
        var interceptedMessage = spy.InterceptedMessages();
        interceptedMessage.ShouldBe(new[]
        {
            "Hi Global 1", "Hi CommandResult 1", "HandlerWithResult", "By CommandResult 1", "By Global 1"
        });
    }
}