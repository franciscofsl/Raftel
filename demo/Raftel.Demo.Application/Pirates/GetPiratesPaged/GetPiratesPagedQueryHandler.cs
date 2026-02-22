using Raftel.Application.Queries;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;
using Raftel.Shared.Extensions;

namespace Raftel.Demo.Application.Pirates.GetPiratesPaged;

internal sealed class GetPiratesPagedQueryHandler(IPirateRepository repository)
    : IQueryHandler<GetPiratesPagedQuery, PagedResult<PiratePageInfo>>
{
    public async Task<Result<PagedResult<PiratePageInfo>>> HandleAsync(
        GetPiratesPagedQuery request,
        CancellationToken token = default)
    {
        var pirates = await repository.ListAllAsync(cancellationToken: token);

        var filtered = pirates
            .WhereIf(!string.IsNullOrEmpty(request.Name),
                _ => ((string)_.Name).Contains(request.Name!, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(_ => new PiratePageInfo(_.Name, _.Bounty))
            .ToList();

        return Result.Success(new PagedResult<PiratePageInfo>(items, totalCount, request.Page, request.PageSize));
    }
}
