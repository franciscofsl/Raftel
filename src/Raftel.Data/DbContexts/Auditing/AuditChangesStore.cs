using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Raftel.Core.Attributes;

namespace Raftel.Data.DbContexts.Auditing;

public class AuditChangesStore
{
    public EntityChangesLog CreateLog(ChangeTracker changeTracker)
    {
        var entries = changeTracker
            .Entries()
            .Where(_ => _.Entity.GetType().GetCustomAttribute(typeof(AuditableAttribute)) != null)
            .Where(_ => _.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        return new EntityChangesLog(entries.Select(EntityChange.Create).ToList());
    }
}