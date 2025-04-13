using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.Tests.Abstractions;

public class MiddlewareTests
{
    public record TestCommand(string Input) : ICommand;

    public class TestHandler : ICommandHandler<TestCommand>
    {
        public Task<Result> HandleAsync(TestCommand request)
            => Task.FromResult(Result.Success());
    }

    public class TracingMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly List<string> _trace;

        public TracingMiddleware(List<string> trace)
        {
            _trace = trace;
        }

        public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
        {
            _trace.Add($"Before {typeof(TRequest).Name}");
            var response = await next();
            _trace.Add($"After {typeof(TRequest).Name}");
            return response;
        }
    }

    public class NamedMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly List<string> _trace;
        private readonly string _name;

        public NamedMiddleware(List<string> trace, string name)
        {
            _trace = trace;
            _name = name;
        }

        public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
        {
            _trace.Add($"Enter {_name}");
            var result = await next();
            _trace.Add($"Exit {_name}");
            return result;
        }
    }

    public class ShortCircuitMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly TResponse _response;

        public ShortCircuitMiddleware(TResponse response)
        {
            _response = response;
        }

        public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next)
        {
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task Middleware_Should_Run_Before_And_After_Handler()
    {
        var trace = new List<string>();

        var services = new ServiceCollection();

        services.AddScoped<IRequestDispatcher, RequestDispatcher>();

        services.AddScoped<IRequestHandler<TestCommand, Result>, TestHandler>();

        services.AddScoped<IRequestMiddleware<TestCommand, Result>>(_ =>
            new TracingMiddleware<TestCommand, Result>(trace));

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IRequestDispatcher>();

        var command = new TestCommand("Check");

        var result = await dispatcher.DispatchAsync<TestCommand, Result>(command);

        result.IsSuccess.ShouldBeTrue();
        trace.ShouldBe(new[]
        {
            "Before TestCommand",
            "After TestCommand"
        });
    }

    [Fact]
    public async Task Multiple_Middleware_Should_Respect_Order()
    {
        var trace = new List<string>();

        var services = new ServiceCollection();

        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<IRequestHandler<TestCommand, Result>, TestHandler>();

        services.AddScoped<IRequestMiddleware<TestCommand, Result>>(_ =>
            new NamedMiddleware<TestCommand, Result>(trace, "First"));
        services.AddScoped<IRequestMiddleware<TestCommand, Result>>(_ =>
            new NamedMiddleware<TestCommand, Result>(trace, "Second"));

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IRequestDispatcher>();

        await dispatcher.DispatchAsync<TestCommand, Result>(new("middleware"));

        trace.ShouldBe(new[]
        {
            "Enter First",
            "Enter Second",
            "Exit Second",
            "Exit First"
        });
    }

    [Fact]
    public async Task Middleware_Can_ShortCircuit_Handler()
    {
        var services = new ServiceCollection();

        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<IRequestHandler<TestCommand, Result>, TestHandler>();

        services.AddScoped<IRequestMiddleware<TestCommand, Result>>(_ =>
            new ShortCircuitMiddleware<TestCommand, Result>(Result.Failure(new Error("STOP", "Intercepted"))));

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IRequestDispatcher>();

        var result = await dispatcher.DispatchAsync<TestCommand, Result>(new("should not reach handler"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("STOP");
    }
}