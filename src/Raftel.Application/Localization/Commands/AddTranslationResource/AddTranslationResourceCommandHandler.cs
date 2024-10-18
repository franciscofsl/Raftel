using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.AddTranslationResource;

public class AddTranslationResourceCommandHandler(ILanguagesRepository languagesRepository)
    : ICommandHandler<AddTranslationResourceCommand, Result<TranslationResource>>
{
    public async Task<Result<TranslationResource>> Handle(AddTranslationResourceCommand command,
        CancellationToken token = default)
    {
        var language = await languagesRepository.GetAsync(command.LanguageId);

        var result = language.AddTranslationResource(command.Key, command.Value);
        if (result.Success)
        {
            await languagesRepository.UpdateAsync(language);
        }

        return result;
    }
}