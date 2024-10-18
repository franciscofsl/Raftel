using Raftel.Core.BaseTypes;
using Raftel.Core.Localization.ValueObjects;

namespace Raftel.Core.Localization;

public sealed class TranslationResource : Entity<TranslationResourceId>
{
    public string Key { get; private set; }
    public string Value { get; private set; }

    private TranslationResource()
    {
    }

    internal static TranslationResource Create(string key, string value)
    {
        return new TranslationResource
        {
            Id = EntityIdGenerator.Create<TranslationResourceId>(),
            Key = key,
            Value = value
        };
    }
}