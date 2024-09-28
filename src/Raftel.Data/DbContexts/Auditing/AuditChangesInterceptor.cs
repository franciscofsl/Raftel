using Microsoft.EntityFrameworkCore.Diagnostics;
using Raftel.Core.Auditing;

namespace Raftel.Data.DbContexts.Auditing;

public class AuditChangesInterceptor(AuditChangesStore store) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var log = store.CreateLog(eventData.Context.ChangeTracker);
        if (log.IsEmpty())
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        eventData.Context.Set<EntityChange>().AddRange(log);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}