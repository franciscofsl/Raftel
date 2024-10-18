using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.DeleteTranslationResource;

public class DeleteTranslationResourceCommandHandler(ILanguagesRepository languagesRepository)
    : ICommandHandler<DeleteTranslationResourceCommand, Result>
{
    public async Task<Result> Handle(DeleteTranslationResourceCommand command,
        CancellationToken token = default)
    {
        var language = await languagesRepository.GetAsync(command.LanguageId);

        language.RemoveTextResource(command.TranslationResourceId);

        return Result.Ok();
    }
}