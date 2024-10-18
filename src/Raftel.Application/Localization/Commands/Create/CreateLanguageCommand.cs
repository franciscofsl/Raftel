using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Commands;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.Create;

public record CreateLanguageCommand : ICommand<Result<LanguageDto>>
{
    public string Name { get; set; }

    public string IsoCode { get; set; }
}