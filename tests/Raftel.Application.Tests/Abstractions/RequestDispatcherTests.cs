using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.Tests.Abstractions;

public sealed class RequestDispatcherTests
{
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
}

public interface ISpy
{
    void Intercept(string message);
    string[] InterceptedMessages();
}

public class Spy : ISpy
{
    private readonly List<string> _interceptedMessages = new();

    public void Intercept(string message)
    {
        _interceptedMessages.Add(message);
    }

    public string[] InterceptedMessages() => _interceptedMessages.ToArray();
}