using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Audit;

namespace Raftel.Application.Features.Audit;

/// <summary>
/// Query handler for retrieving entity audit history.
/// </summary>
public sealed class GetEntityAuditHistoryQueryHandler : IQueryHandler<GetEntityAuditHistoryQuery, List<AuditEntryDto>>
{
    private readonly IAuditRepository _auditRepository;

    public GetEntityAuditHistoryQueryHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result<List<AuditEntryDto>>> HandleAsync(GetEntityAuditHistoryQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEntries = await _auditRepository.GetEntityAuditHistoryAsync(
                query.EntityName,
                query.EntityId,
                cancellationToken);

            var auditDtos = auditEntries.Select(MapToDto).ToList();

            return Result.Success(auditDtos);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<AuditEntryDto>>(
                new Error("AuditHistory.QueryFailed", $"Failed to retrieve audit history: {ex.Message}"));
        }
    }

    private static AuditEntryDto MapToDto(AuditEntry auditEntry)
    {
        return new AuditEntryDto
        {
            Date = auditEntry.Timestamp,
            ChangeType = auditEntry.ChangeType,
            EntityName = auditEntry.EntityName,
            EntityId = auditEntry.EntityId,
            Details = auditEntry.Details,
            Changes = auditEntry.PropertyChanges.Select(pc => new AuditPropertyChangeDto
            {
                PropertyName = pc.PropertyName,
                OldValue = pc.OldValue,
                NewValue = pc.NewValue
            }).ToList()
        };
    }
}