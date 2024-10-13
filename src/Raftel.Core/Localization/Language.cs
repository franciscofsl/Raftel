using Raftel.Core.BaseTypes;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Core.Localization;

public sealed class Language : AggregateRoot<LanguageId>
{
    private Language()
    {
    }

    public static Language Create()
    {
        return new Language
        {
            Id = EntityIdGenerator.Create<LanguageId>()
        };
    }
}