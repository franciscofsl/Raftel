using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Auditing;

namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Default <see cref="IAuditStore"/> implementation.
/// </summary>
internal sealed class AuditStore : IAuditStore
{
    public void Save(DbContext context, AuditLog auditLog)
    {
        context.Add(auditLog);
    }
}
