using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Application.Localization.Commands.Delete;

public record DeleteLanguageCommand(LanguageId Id) : ICommand<bool>;