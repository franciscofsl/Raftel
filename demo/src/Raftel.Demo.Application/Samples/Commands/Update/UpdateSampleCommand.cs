using Raftel.Application.Cqrs.Commands;
using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Shared.Results;

namespace Raftel.Demo.Application.Samples.Commands.Update;

public record UpdateSampleCommand : ICommand<Result<SampleDto>>
{
    public Guid Id { get; set; }
}