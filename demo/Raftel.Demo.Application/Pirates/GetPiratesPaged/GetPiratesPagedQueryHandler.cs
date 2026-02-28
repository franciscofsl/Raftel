using Raftel.Application.Queries;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;

namespace Raftel.Demo.Application.Pirates.GetPiratesPaged;

internal sealed class GetPiratesPagedQueryHandler(IPirateRepository repository)
    : IQueryHandler<GetPiratesPagedQuery, PagedResult<PiratePageInfo>>
{
    public async Task<Result<PagedResult<PiratePageInfo>>> HandleAsync(
        GetPiratesPagedQuery request,
        CancellationToken token = default)
    {
        var (pirates, totalCount) = await repository.SearchPagedAsync(
            request.Page, request.PageSize, request.Name, token);

        var items = pirates
            .Select(_ => new PiratePageInfo(_.Name, _.Bounty))
            .ToList();

        return Result.Success(new PagedResult<PiratePageInfo>(items, totalCount, request.Page, request.PageSize));
    }
}
