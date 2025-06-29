using Raftel.Application.Queries;

namespace Raftel.Application.Features.Audit;

/// <summary>
/// Query to get audit history for a specific entity.
/// </summary>
/// <param name="EntityName">The name of the entity.</param>
/// <param name="EntityId">The identifier of the entity.</param>
public sealed record GetEntityAuditHistoryQuery(
    string EntityName,
    string EntityId) : IQuery<List<AuditEntryDto>>;