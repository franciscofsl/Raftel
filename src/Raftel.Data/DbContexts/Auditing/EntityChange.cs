using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Raftel.Core.Attributes;

namespace Raftel.Data.DbContexts.Auditing;

public class EntityChange
{
    [ExcludeFromCodeCoverage]
    private EntityChange()
    {
        /* For ORM */
    }

    public DateTime OccurredOn { get; private set; }
    public string EntityId { get; private set; }
    public AuditEventKind Kind { get; private set; }

    public static EntityChange Create(EntityEntry entry)
    {
        entry.CurrentValues.TryGetValue<object>("Id", out var idValue);

        return new EntityChange()
        {
            OccurredOn = DateTime.UtcNow,
            EntityId = idValue?.ToString(),
            Kind = entry.State.ToKind()
        };
    }
}