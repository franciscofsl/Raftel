using Raftel.Application.Cqrs.Commands;
using Raftel.Demo.Core.Samples;

namespace Raftel.Demo.Application.Samples.Commands.Delete;

public class DeleteSampleCommandHandler(ISamplesRepository samplesRepository) : ICommandHandler<DeleteSampleCommand, bool>
{
    public async Task<bool> Handle(DeleteSampleCommand command, CancellationToken token = default)
    {
        var sample = await samplesRepository.GetAsync(command.Id);
        
        await samplesRepository.DeleteAsync(sample);

        return true;
    }
}