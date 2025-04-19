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
}