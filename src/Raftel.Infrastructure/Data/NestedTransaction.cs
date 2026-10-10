using Raftel.Application;

namespace Raftel.Infrastructure.Data;

/// <summary>
/// No-op transaction handle returned when a transaction is begun while another is already active
/// on the same unit of work. Only one physical transaction exists per unit of work at a time.
/// </summary>
internal sealed class NestedTransaction(RaftelTransaction root) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        root.MarkRollbackOnly();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
