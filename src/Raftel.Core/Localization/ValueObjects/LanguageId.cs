using Raftel.Core.BaseTypes;

namespace Raftel.Core.Localization.ValueObjects;

public sealed record LanguageId : EntityId
{
    public static explicit operator LanguageId(Guid id) => new()
    {
        Value = id
    };

    public static implicit operator Guid(LanguageId id) => id.Value;
}