using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.Update;

public class UpdateLanguageCommandHandler(ILanguagesRepository languagesRepository)
    : ICommandHandler<UpdateLanguageCommand, Result<LanguageDto>>
{
    public async Task<Result<LanguageDto>> Handle(UpdateLanguageCommand command, CancellationToken token = default)
    {
        var language = await languagesRepository.GetAsync((LanguageId)command.Id);

        language.Name = command.Name;

        await languagesRepository.UpdateAsync(language);

        return Result.Ok(new LanguageDto()
        {
            Id = language.Id,
            Name = language.Name,
            IsoCode = language.IsoCode
        });
    }
}