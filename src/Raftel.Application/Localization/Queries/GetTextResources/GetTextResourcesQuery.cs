using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Queries;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Application.Localization.Queries.GetTextResources;

public record GetTextResourcesQuery(LanguageId LanguageId) : IQuery<List<TextResourceDto>>;