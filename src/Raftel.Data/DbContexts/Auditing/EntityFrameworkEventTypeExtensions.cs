using Microsoft.EntityFrameworkCore;
using Raftel.Core.Attributes;

namespace Raftel.Data.DbContexts.Auditing;

internal static class EntityFrameworkEventTypeExtensions
{
    public static AuditEventKind ToKind(this EntityState entityState)
    {
        return entityState switch
        {
            EntityState.Added => AuditEventKind.Created,
            EntityState.Deleted => AuditEventKind.Deleted,
            EntityState.Modified => AuditEventKind.Updated,
            _ => AuditEventKind.Created
        };
    }
}