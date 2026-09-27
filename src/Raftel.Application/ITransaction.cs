namespace Raftel.Application;

/// <summary>
/// Represents an explicit atomic boundary opened on an <see cref="IUnitOfWork"/>.
/// </summary>
/// <remarks>
/// Committing or rolling back always runs to completion regardless of cancellation: cancelling
/// mid-commit or mid-rollback would leave the transaction in an indeterminate state. Disposing the
/// transaction without having committed or rolled it back first rolls it back.
/// </remarks>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction, persisting every change made within it.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Ignored: commit is not cancellable.
    /// </param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction, discarding every change made within it.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Ignored: rollback is not cancellable.
    /// </param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
