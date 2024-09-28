using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Raftel.Data.DbContexts.Auditing;

public class AuditChangesStore
{
    public EntityChangesLog CreateLog(ChangeTracker changeTracker)
    {

        return new EntityChangesLog([]);
    }
}