using Raftel.Application.Cqrs.Queries;
using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Demo.Core.Samples;

namespace Raftel.Demo.Application.Samples.Queries.ById;

public class GetSamplesQueryHandler(ISamplesRepository repository) : IQueryHandler<GetSampleByIdQuery, SampleDto>
{
    public async Task<SampleDto> Handle(GetSampleByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var sample = await repository.GetAsync(query.Id);

        return sample.ToDto();
    }
}