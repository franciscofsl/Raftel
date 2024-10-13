using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Queries;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Application.Localization.Queries.ById;

public record GetLanguageByIdQuery(LanguageId Id) : IQuery<LanguageDto>;