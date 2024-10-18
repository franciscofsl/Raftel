using Microsoft.EntityFrameworkCore;
using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Data.DbContexts;

namespace Raftel.Data.Repositories.Localization;

public class LanguagesRepository(IDbContextFactory dbContextFactory)
    : EfRepository<Language, LanguageId>(dbContextFactory), ILanguagesRepository
{
    protected override IQueryable<Language> ApplyIncludes(IQueryable<Language> query)
    {
        return query.Include(_ => _.Resources);
    }
}