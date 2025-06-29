namespace Raftel.Domain.Features.Audit;

/// <summary>
/// Repository interface for audit operations.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Gets audit entries for a specific entity.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="entityId">The identifier of the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of audit entries for the entity.</returns>
    Task<List<AuditEntry>> GetEntityAuditHistoryAsync(
        string entityName, 
        string entityId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries within a date range.
    /// </summary>
    /// <param name="fromDate">The start date.</param>
    /// <param name="toDate">The end date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of audit entries within the date range.</returns>
    Task<List<AuditEntry>> GetAuditHistoryByDateRangeAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries by change type.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of audit entries of the specified change type.</returns>
    Task<List<AuditEntry>> GetAuditHistoryByChangeTypeAsync(
        string changeType, 
        CancellationToken cancellationToken = default);
}