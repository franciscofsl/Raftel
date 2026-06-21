using NSubstitute;
using Raftel.Application.Abstractions;
using Raftel.Application.Abstractions.Auditing;
using Raftel.Application.Middlewares;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class AuditLogMiddlewareTests
{
    private readonly IAuditLogScope _auditLogScope;
    private readonly AuditLogMiddleware<SampleCommand, string> _middleware;

    public AuditLogMiddlewareTests()
    {
        _auditLogScope = Substitute.For<IAuditLogScope>();
        _middleware = new AuditLogMiddleware<SampleCommand, string>(_auditLogScope);
    }

    [Fact]
    public async Task HandleAsync_ShouldBeginScope_WithRequestFullTypeName()
    {
        var scope = Substitute.For<IDisposable>();
        _auditLogScope.Begin(Arg.Any<string>()).Returns(scope);

        await _middleware.HandleAsync(new SampleCommand(), () => Task.FromResult("Result"));

        _auditLogScope.Received(1).Begin(typeof(SampleCommand).FullName!);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNextResult()
    {
        _auditLogScope.Begin(Arg.Any<string>()).Returns(Substitute.For<IDisposable>());

        var result = await _middleware.HandleAsync(new SampleCommand(), () => Task.FromResult("Result"));

        result.ShouldBe("Result");
    }

    [Fact]
    public async Task HandleAsync_ShouldDisposeScope_AfterNextCompletes()
    {
        var scope = Substitute.For<IDisposable>();
        _auditLogScope.Begin(Arg.Any<string>()).Returns(scope);

        await _middleware.HandleAsync(new SampleCommand(), () => Task.FromResult("Result"));

        scope.Received(1).Dispose();
    }

    [Fact]
    public async Task HandleAsync_ShouldDisposeScope_EvenWhenNextThrows()
    {
        var scope = Substitute.For<IDisposable>();
        _auditLogScope.Begin(Arg.Any<string>()).Returns(scope);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _middleware.HandleAsync(new SampleCommand(),
                () => throw new InvalidOperationException("Handler failed")));

        scope.Received(1).Dispose();
    }

    private sealed class SampleCommand : IRequest<string>
    {
    }
}
