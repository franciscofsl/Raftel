using Raftel.Application.Cqrs.Commands;
using Raftel.Application.Localization.Commands.AddTranslationResource;
using Raftel.Core.Localization;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.UpdateTranslationResource;

public class UpdateTranslationResourceCommandHandler(ILanguagesRepository languagesRepository)
    : ICommandHandler<AddTranslationResourceCommand, Result<TranslationResource>>
{
    public async Task<Result<TranslationResource>> Handle(AddTranslationResourceCommand command,
        CancellationToken token = default)
    {
        var language = await languagesRepository.GetAsync(command.LanguageId);

        return language.AddTranslationResource(command.Key, command.Value);
    }
}