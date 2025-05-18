using Raftel.Application.Abstractions;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Abstractions.Middlewares;

public sealed class UnitOfWorkMiddlewareTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UnitOfWorkMiddleware<TestCommand> _handler;

    public UnitOfWorkMiddlewareTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new UnitOfWorkMiddleware<TestCommand>(_unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandSucceeds_ShouldCommitUnitOfWork()
    {
        var command = new TestCommand();
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        var result = await _handler.HandleAsync(command, next);

        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFails_ShouldNotCommitUnitOfWork()
    {
        var command = new TestCommand();
        var error = new Error("BOMB_FAILURE", "Buggy broke the command into pieces.");
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Failure(error));

        var result = await _handler.HandleAsync(command, next);

        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().CommitAsync();
    }

    private sealed record TestCommand : ICommand;
}