using Raftel.Application.Cqrs.Commands;
using Raftel.Demo.Application.Contracts.Samples;
using Raftel.Demo.Core.Samples;
using Raftel.Demo.Core.Samples.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Demo.Application.Samples.Commands.Update;

public class UpdateSampleCommandHandler(ISamplesRepository samplesRepository)
    : ICommandHandler<UpdateSampleCommand, Result<SampleDto>>
{
    public async Task<Result<SampleDto>> Handle(UpdateSampleCommand command, CancellationToken token = default)
    {
        var sample = await samplesRepository.GetAsync((SampleId)command.Id);

        await samplesRepository.UpdateAsync(sample);

        return Result.Ok(sample.ToDto());
    }
}