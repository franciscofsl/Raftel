using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Auditing;

namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Persists an <see cref="AuditLog"/> aggregate as part of an ongoing EF Core save operation.
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// Adds the given <see cref="AuditLog"/> to the change tracker of <paramref name="context"/>
    /// so that it is persisted within the same <c>SaveChanges</c> call as the audited changes.
    /// </summary>
    void Save(DbContext context, AuditLog auditLog);
}
