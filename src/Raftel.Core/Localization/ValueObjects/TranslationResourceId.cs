using Raftel.Core.BaseTypes;

namespace Raftel.Core.Localization.ValueObjects;

public sealed record TranslationResourceId : EntityId
{
    public static explicit operator TranslationResourceId(Guid id) => new()
    {
        Value = id
    };

    public static implicit operator Guid(TranslationResourceId id) => id.Value;
}