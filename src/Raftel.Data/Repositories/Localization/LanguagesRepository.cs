using Raftel.Core.Localization;
using Raftel.Core.Localization.ValueObjects;
using Raftel.Data.DbContexts;

namespace Raftel.Data.Repositories.Localization;

public class LanguagesRepository(IDbContextFactory dbContextFactory)
    : EfRepository<Language, LanguageId>(dbContextFactory), ILanguagesRepository;