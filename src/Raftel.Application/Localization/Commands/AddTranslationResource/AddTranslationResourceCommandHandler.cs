using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.AddTranslationResource;

public class AddTranslationResourceCommandHandler(ILanguagesRepository languagesRepository)
    : ICommandHandler<AddTranslationResourceCommand, Result>
{
    public async Task<Result> Handle(AddTranslationResourceCommand command, CancellationToken token = default)
    {
        var language = await languagesRepository.GetAsync(command.LanguageId);

        return language.AddTranslationResource(command.Key, command.Value);
    }
}