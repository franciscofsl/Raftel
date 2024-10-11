using Raftel.Application.Cqrs.Commands;
using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Demo.Core.Samples;
using Raftel.Shared.Results;

namespace Raftel.Demo.Application.Samples.Commands.Create;

public class CreateSampleCommandHandler(ISamplesRepository samplesRepository)
    : ICommandHandler<CreateSampleCommand, Result<SampleDto>>
{
    public async Task<Result<SampleDto>> Handle(CreateSampleCommand command, CancellationToken token = default)
    {
        var sample = Sample.Create();
        await samplesRepository.InsertAsync(sample);
        return Result.Ok(sample.ToDto());
    }
}