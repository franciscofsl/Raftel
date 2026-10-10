using Microsoft.EntityFrameworkCore.Storage;
using Raftel.Application;

namespace Raftel.Infrastructure.Data;

/// <summary>
/// Wraps the EF Core <see cref="IDbContextTransaction"/> backing the outermost, physical
/// transaction opened for a unit of work.
/// </summary>
internal sealed class RaftelTransaction(IDbContextTransaction transaction, Action onCompleted) : ITransaction
{
    private bool _rollbackOnly;
    private bool _completed;

    public void MarkRollbackOnly() => _rollbackOnly = true;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_rollbackOnly)
        {
            await RollbackCoreAsync();
            throw new InvalidOperationException(
                "The transaction was marked rollback-only by a nested transaction and cannot be committed.");
        }

        if (_completed)
        {
            return;
        }

        await transaction.CommitAsync(CancellationToken.None);
        Complete();
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default) => RollbackCoreAsync();

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            await RollbackCoreAsync();
        }

        await transaction.DisposeAsync();
    }

    private async Task RollbackCoreAsync()
    {
        if (_completed)
        {
            return;
        }

        await transaction.RollbackAsync(CancellationToken.None);
        Complete();
    }

    private void Complete()
    {
        _completed = true;
        onCompleted();
    }
}
