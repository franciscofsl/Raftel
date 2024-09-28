using Raftel.Core.BaseTypes;

namespace Raftel.Core.CustomEntities.ValueObjects;

public sealed record CustomEntityConfigurationId : EntityId
{
    public static explicit operator CustomEntityConfigurationId(Guid id) => new()
    {
        Value = id
    };

    public static implicit operator Guid(CustomEntityConfigurationId id) => id.Value;
}