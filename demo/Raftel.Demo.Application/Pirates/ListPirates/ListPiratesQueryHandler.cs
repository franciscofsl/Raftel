using Raftel.Application;
using Raftel.Application.Queries;
using Raftel.Demo.Domain.Pirates;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Specifications;

namespace Raftel.Demo.Application.Pirates.ListPirates;

internal sealed class ListPiratesQueryHandler(IPirateRepository repository, PaginationOptions paginationOptions)
    : IQueryHandler<ListPiratesQuery, PagedResult<PirateSummary>>
{
    private static readonly SortMap<Pirate> SortMap = new SortMap<Pirate>()
        .Allow("name", pirate => pirate.Name)
        .Allow("bounty", pirate => pirate.Bounty);

    public async Task<Result<PagedResult<PirateSummary>>> HandleAsync(ListPiratesQuery request,
        CancellationToken token = default)
    {
        var pageResult = PageRequest.Create(request.Page ?? 1, request.PageSize ?? paginationOptions.DefaultPageSize);
        if (pageResult.IsFailure)
        {
            return Result.Failure<PagedResult<PirateSummary>>(pageResult.Error);
        }

        var sortRequestsResult = SortRequest.Parse(request.Sort);
        if (sortRequestsResult.IsFailure)
        {
            return Result.Failure<PagedResult<PirateSummary>>(sortRequestsResult.Error);
        }

        var sortSelectorsResult = SortMap.ResolveAll(sortRequestsResult.Value);
        if (sortSelectorsResult.IsFailure)
        {
            return Result.Failure<PagedResult<PirateSummary>>(sortSelectorsResult.Error);
        }

        var pirates = await repository.ListPagedAsync(pageResult.Value, sort: sortSelectorsResult.Value,
            cancellationToken: token);

        return pirates.Map(pirate => new PirateSummary(pirate.Name, pirate.Bounty));
    }
}
