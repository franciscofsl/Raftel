using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Queries;
using Raftel.Core.Localization;

namespace Raftel.Application.Localization.Queries.ById;

public class GetLanguagesByIdQueryHandler(ILanguagesRepository repository)
    : IQueryHandler<GetLanguageByIdQuery, LanguageDto>
{
    public async Task<LanguageDto> Handle(GetLanguageByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var language = await repository.GetAsync(query.Id);

        return new LanguageDto()
        {
            IsoCode = language.IsoCode,
            Name = language.Name,
            Id = language.Id
        };
    }
}