using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;

namespace Raftel.Application.Localization.Commands.Delete;

public class DeleteLanguageCommandHandler(ILanguagesRepository languagesRepository) : ICommandHandler<DeleteLanguageCommand, bool>
{
    public async Task<bool> Handle(DeleteLanguageCommand command, CancellationToken token = default)
    {
        var language = await languagesRepository.GetAsync(command.Id);
        
        await languagesRepository.DeleteAsync(language);

        return true;
    }
}