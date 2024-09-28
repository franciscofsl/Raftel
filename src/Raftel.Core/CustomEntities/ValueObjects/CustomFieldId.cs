using Raftel.Core.BaseTypes;

namespace Raftel.Core.CustomEntities.ValueObjects;

public sealed record CustomFieldId : EntityId
{
    public static explicit operator CustomFieldId(Guid id) => new()
    {
        Value = id
    };

    public static implicit operator Guid(CustomFieldId id) => id.Value;
}