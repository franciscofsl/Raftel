using Microsoft.EntityFrameworkCore;
using Raftel.Infrastructure.Data.Audit;

namespace Raftel.Infrastructure.Data.Repositories.Audit;

/// <summary>
/// Repository implementation for audit operations.
/// </summary>
internal class AuditRepository<TDbContext> : IAuditRepository 
    where TDbContext : RaftelDbContext<TDbContext>
{
    private readonly TDbContext _dbContext;

    public AuditRepository(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AuditEntry>> GetEntityAuditHistoryAsync(
        string entityName, 
        string entityId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<AuditEntry>()
            .Include(x => x.PropertyChanges)
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditEntry>> GetAuditHistoryByDateRangeAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<AuditEntry>()
            .Include(x => x.PropertyChanges)
            .Where(x => x.Timestamp >= fromDate && x.Timestamp <= toDate)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditEntry>> GetAuditHistoryByChangeTypeAsync(
        string changeType, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<AuditEntry>()
            .Include(x => x.PropertyChanges)
            .Where(x => x.ChangeType == changeType)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }
}