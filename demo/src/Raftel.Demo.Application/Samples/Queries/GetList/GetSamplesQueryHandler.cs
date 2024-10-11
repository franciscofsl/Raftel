using Raftel.Application.Cqrs.Queries;
using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Demo.Core.Samples;

namespace Raftel.Demo.Application.Samples.Queries.GetList;

public class GetSamplesQueryHandler(ISamplesRepository repository)
    : IQueryHandler<GetSamplesQuery, List<SampleForListDto>>
{
    public Task<List<SampleForListDto>> Handle(GetSamplesQuery query, CancellationToken cancellationToken = default)
    {
        return repository.GetListAsync(_ => new SampleForListDto
        {
        });
    }
}