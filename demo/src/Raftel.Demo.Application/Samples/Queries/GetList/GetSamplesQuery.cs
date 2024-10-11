using Raftel.Application.Cqrs.Queries;
using Raftel.Demo.Application.Contracts.Samples;

namespace Raftel.Demo.Application.Samples.Queries.GetList;

public record GetSamplesQuery : IQuery<List<SampleForListDto>>;