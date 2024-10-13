using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Queries;
using Raftel.Core.Localization;

namespace Raftel.Application.Localization.Queries.GetList;

public class GetLanguagesQueryHandler(ILanguagesRepository repository)
    : IQueryHandler<GetLanguagesQuery, List<LanguageDto>>
{
    public Task<List<LanguageDto>> Handle(GetLanguagesQuery query, CancellationToken cancellationToken = default)
    {
        return repository.GetListAsync(_ => new LanguageDto
        {
            IsoCode = _.IsoCode,
            Name = _.Name,
            Id = _.Id
        });
    }
}