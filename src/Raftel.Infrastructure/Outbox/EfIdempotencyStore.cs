using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.Outbox;
using Raftel.Domain.Outbox.ValueObjects;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Outbox;

/// <summary>
/// Stores handler receipts through the unit of work's own <c>DbContext</c>, so a receipt commits or rolls
/// back together with the handler's writes.
/// </summary>
internal sealed class EfIdempotencyStore<TDbContext> : IIdempotencyStore
    where TDbContext : RaftelDbContext<TDbContext>
{
    private readonly TDbContext _context;
    private readonly TimeProvider _timeProvider;

    public EfIdempotencyStore(TDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public Task<bool> HasCompletedAsync(OutboxMessageId messageId, string handlerName,
        CancellationToken cancellationToken = default) =>
        _context.Set<OutboxHandlerReceipt>()
            .AsNoTracking()
            .AnyAsync(receipt => receipt.MessageId == messageId && receipt.Handler == handlerName, cancellationToken);

    public async Task MarkCompletedAsync(OutboxMessageId messageId, string handlerName,
        CancellationToken cancellationToken = default)
    {
        _context.Set<OutboxHandlerReceipt>().Add(
            new OutboxHandlerReceipt(messageId, handlerName, _timeProvider.GetUtcNow()));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
