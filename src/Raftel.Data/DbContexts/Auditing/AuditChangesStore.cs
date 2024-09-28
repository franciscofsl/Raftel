using System.Reflection;
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
            .ToList();
        
        return new EntityChangesLog(entries.Select(_ => new EntityChange()).ToList());
    }
}