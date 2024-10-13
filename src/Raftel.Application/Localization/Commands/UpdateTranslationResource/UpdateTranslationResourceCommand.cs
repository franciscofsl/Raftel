using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.UpdateTranslationResource;

public record UpdateTranslationResourceCommand(LanguageId LanguageId, string Key, string Value) : ICommand<Result<TranslationResource>>;