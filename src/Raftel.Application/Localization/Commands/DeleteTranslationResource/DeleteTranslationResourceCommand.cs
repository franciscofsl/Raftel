using Raftel.Application.Cqrs.Commands;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Shared.Results;

namespace Raftel.Application.Localization.Commands.DeleteTranslationResource;

public record DeleteTranslationResourceCommand(LanguageId LanguageId, TranslationResourceId TranslationResourceId) : ICommand<Result>;