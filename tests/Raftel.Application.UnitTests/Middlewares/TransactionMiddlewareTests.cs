using NSubstitute;
using Raftel.Application;
using Raftel.Application.Commands;
using Raftel.Application.Middlewares;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Middlewares;

public class TransactionMiddlewareTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransaction _transaction;
    private readonly TransactionMiddleware<SampleCommand> _middleware;

    public TransactionMiddlewareTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _transaction = Substitute.For<ITransaction>();
        _unitOfWork.HasActiveTransaction.Returns(false);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

        _middleware = new TransactionMiddleware<SampleCommand>(_unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WhenResultSucceeds_ShouldCommitOnce_AndNeverRollback()
    {
        var result = await _middleware.HandleAsync(new SampleCommand(), _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenResultFails_ShouldRollback_AndReturnFailureUnchanged()
    {
        var error = Error.Validation("Sample.Invalid", "The sample was invalid");

        var result = await _middleware.HandleAsync(new SampleCommand(), _ => Task.FromResult(Result.Failure(error)),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNextThrows_ShouldRollback_AndRethrowOriginalException()
    {
        var exception = new InvalidOperationException("Handler failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _middleware.HandleAsync(new SampleCommand(), _ => throw exception, CancellationToken.None));

        thrown.ShouldBeSameAs(exception);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTransactionAlreadyActive_ShouldNotBeginCommitOrRollback()
    {
        _unitOfWork.HasActiveTransaction.Returns(true);
        var nextCalled = false;

        var result = await _middleware.HandleAsync(new SampleCommand(), _ =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        nextCalled.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenResultSucceeds_ShouldCommit_WithNoneToken_EvenIfRequestTokenIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await _middleware.HandleAsync(new SampleCommand(), _ => Task.FromResult(Result.Success()), cts.Token);

        await _transaction.Received(1).CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_WhenResultFails_ShouldRollback_WithNoneToken_EvenIfRequestTokenIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var error = Error.Validation("Sample.Invalid", "The sample was invalid");

        await _middleware.HandleAsync(new SampleCommand(), _ => Task.FromResult(Result.Failure(error)), cts.Token);

        await _transaction.Received(1).RollbackAsync(CancellationToken.None);
    }

    private sealed record SampleCommand : ICommand;
}

public class TransactionMiddlewareOfTResultTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransaction _transaction;
    private readonly TransactionMiddleware<SampleCommand, string> _middleware;

    public TransactionMiddlewareOfTResultTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _transaction = Substitute.For<ITransaction>();
        _unitOfWork.HasActiveTransaction.Returns(false);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

        _middleware = new TransactionMiddleware<SampleCommand, string>(_unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WhenResultSucceeds_ShouldCommitOnce_AndReturnTypedValueUnchanged()
    {
        var result = await _middleware.HandleAsync(new SampleCommand(),
            _ => Task.FromResult(Result.Success("value")), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("value");
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenResultFails_ShouldRollback_AndReturnFailureUnchanged()
    {
        var error = Error.Validation("Sample.Invalid", "The sample was invalid");

        var result = await _middleware.HandleAsync(new SampleCommand(),
            _ => Task.FromResult(Result.Failure<string>(error)), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNextThrows_ShouldRollback_AndRethrowOriginalException()
    {
        var exception = new InvalidOperationException("Handler failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _middleware.HandleAsync(new SampleCommand(), _ => throw exception, CancellationToken.None));

        thrown.ShouldBeSameAs(exception);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTransactionAlreadyActive_ShouldNotBeginCommitOrRollback()
    {
        _unitOfWork.HasActiveTransaction.Returns(true);

        var result = await _middleware.HandleAsync(new SampleCommand(),
            _ => Task.FromResult(Result.Success("value")), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    private sealed record SampleCommand : ICommand<string>;
}
