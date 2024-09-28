using Raftel.Core.BaseTypes;

namespace Raftel.Core.CustomEntities.ValueObjects;

public sealed record CustomEntityId : EntityId
{
    public static explicit operator CustomEntityId(Guid id) => new()
    {
        Value = id
    };

    public static implicit operator Guid(CustomEntityId id) => id.Value;
}