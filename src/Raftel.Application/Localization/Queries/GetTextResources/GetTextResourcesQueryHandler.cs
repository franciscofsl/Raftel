using Raftel.Application.Contracts.Localization;
using Raftel.Application.Cqrs.Queries;
using Raftel.Core.Localization;

namespace Raftel.Application.Localization.Queries.GetTextResources;

public class GetTextResourcesQueryHandler(ILanguagesRepository repository)
    : IQueryHandler<GetTextResourcesQuery, List<TextResourceDto>>
{
    public async Task<List<TextResourceDto>> Handle(GetTextResourcesQuery query,
        CancellationToken cancellationToken = default)
    {
        var language = await repository.GetAsync(query.LanguageId);

        return language.Resources
            .Select(_ => new TextResourceDto
            {
                Id = _.Id,
                LanguageId = language.Id.Value,
                Key = _.Key,
                Value = _.Value
            })
            .ToList();
    }
}