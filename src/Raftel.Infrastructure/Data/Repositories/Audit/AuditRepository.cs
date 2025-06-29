using Microsoft.EntityFrameworkCore;
using Raftel.Domain.Features.Audit;
using Raftel.Infrastructure.Data.Audit;
using DomainAuditEntry = Raftel.Domain.Features.Audit.AuditEntry;
using DomainAuditPropertyChange = Raftel.Domain.Features.Audit.AuditPropertyChange;
using InfrastructureAuditEntry = Raftel.Infrastructure.Data.Audit.AuditEntry;

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

    public async Task<List<DomainAuditEntry>> GetEntityAuditHistoryAsync(
        string entityName, 
        string entityId, 
        CancellationToken cancellationToken = default)
    {
        var infrastructureEntries = await _dbContext.Set<InfrastructureAuditEntry>()
            .Include(x => x.PropertyChanges)
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);

        return infrastructureEntries.Select(MapToDomain).ToList();
    }

    public async Task<List<DomainAuditEntry>> GetAuditHistoryByDateRangeAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        var infrastructureEntries = await _dbContext.Set<InfrastructureAuditEntry>()
            .Include(x => x.PropertyChanges)
            .Where(x => x.Timestamp >= fromDate && x.Timestamp <= toDate)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);

        return infrastructureEntries.Select(MapToDomain).ToList();
    }

    public async Task<List<DomainAuditEntry>> GetAuditHistoryByChangeTypeAsync(
        string changeType, 
        CancellationToken cancellationToken = default)
    {
        var infrastructureEntries = await _dbContext.Set<InfrastructureAuditEntry>()
            .Include(x => x.PropertyChanges)
            .Where(x => x.ChangeType == changeType)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);

        return infrastructureEntries.Select(MapToDomain).ToList();
    }

    private static DomainAuditEntry MapToDomain(InfrastructureAuditEntry infrastructureEntry)
    {
        return new DomainAuditEntry(
            infrastructureEntry.Timestamp,
            infrastructureEntry.ChangeType,
            infrastructureEntry.EntityName,
            infrastructureEntry.EntityId,
            infrastructureEntry.Details,
            infrastructureEntry.PropertyChanges.Select(pc => new DomainAuditPropertyChange(
                pc.PropertyName,
                pc.OldValue,
                pc.NewValue
            )).ToList()
        );
    }
}