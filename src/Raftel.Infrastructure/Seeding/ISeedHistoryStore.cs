namespace Raftel.Infrastructure.Seeding;

internal interface ISeedHistoryStore
{
    Task<int?> GetAppliedVersionAsync(string name, Guid? tenantId, CancellationToken cancellationToken);

    Task RecordAsync(string name, Guid? tenantId, int version, CancellationToken cancellationToken);
}
