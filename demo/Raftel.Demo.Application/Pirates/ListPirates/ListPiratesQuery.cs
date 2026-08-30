using Raftel.Application.Authorization;
using Raftel.Application.Queries;

namespace Raftel.Demo.Application.Pirates.ListPirates;

/// <summary>
/// Reference example of a paginated query: sortable by "name" or "bounty" (prefix a field
/// with "-" for descending order, e.g. "sort=-bounty"), defaulting page size from
/// <see cref="Raftel.Application.PaginationOptions"/> when not specified by the caller.
/// </summary>
[RequiresPermission(PiratesPermissions.View)]
public sealed record ListPiratesQuery(int? Page, int? PageSize, string Sort) : IPagedQuery<PirateSummary>;
