using Raftel.Application.Cqrs.Queries;
using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Demo.Core.Samples.ValueObjects;

namespace Raftel.Demo.Application.Samples.Queries.ById;

public record GetSampleByIdQuery(SampleId Id) : IQuery<SampleDto>;