using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Commands;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.Update;

public record UpdateLanguageCommand : ICommand<Result<LanguageDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}