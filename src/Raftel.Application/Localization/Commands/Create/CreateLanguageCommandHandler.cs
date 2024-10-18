using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.Create;

public class CreateLanguageCommandHandler(ILanguagesRepository languagesRepository)
    : ICommandHandler<CreateLanguageCommand, Result<LanguageDto>>
{
    public async Task<Result<LanguageDto>> Handle(CreateLanguageCommand command, CancellationToken token = default)
    {
        var language = Language.Create(command.Name, command.IsoCode);
        await languagesRepository.InsertAsync(language);
        return Result.Ok(new LanguageDto()
        {
            IsoCode = language.IsoCode,
            Name = language.Name,
            Id = language.Id
        });
    }
}