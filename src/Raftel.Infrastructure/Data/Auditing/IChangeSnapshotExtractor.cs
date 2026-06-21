using Microsoft.EntityFrameworkCore;

namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Reads the EF Core change tracker and produces EF Core-agnostic snapshots for every
/// audited entity that was added, modified or deleted. This is the only audit component
/// directly coupled to EF Core.
/// </summary>
public interface IChangeSnapshotExtractor
{
    /// <summary>
    /// Extracts a snapshot for each audited entity tracked by <paramref name="context"/> that
    /// has pending changes.
    /// </summary>
    IReadOnlyList<EntityChangeSnapshot> Extract(DbContext context);
}
