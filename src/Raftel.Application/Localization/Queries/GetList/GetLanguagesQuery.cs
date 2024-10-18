using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Queries;

namespace Raftel.Application.Localization.Queries.GetList;

public record GetLanguagesQuery : IQuery<List<LanguageDto>>;