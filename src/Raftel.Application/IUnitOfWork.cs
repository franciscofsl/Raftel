namespace Raftel.Application;

/// <summary>
/// Represents a unit of work that encapsulates a series of operations 
/// to be committed as a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all changes made within the unit of work asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task that represents the asynchronous commit operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an explicit transaction, opening a new physical transaction if none is active,
    /// or a no-op nested transaction if one already is.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A transaction handle scoped to this unit of work.</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether a transaction is currently active on this unit of work.
    /// </summary>
    bool HasActiveTransaction { get; }
}