using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Domain.Pirates;

public interface IPirateRepository : IRepository<Pirate, PirateId>
{
    /// <summary>
    /// Retrieves a paginated list of pirates with an optional name filter applied at the database level.
    /// </summary>
    Task<(IReadOnlyList<Pirate> Items, int TotalCount)> SearchPagedAsync(int page, int pageSize,
        string nameFilter = null, CancellationToken cancellationToken = default);
}