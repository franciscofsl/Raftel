using Raftel.Core.BaseTypes;

namespace Raftel.Demo.Core.Samples.ValueObjects;

public sealed record SampleId : EntityId
{
    public static explicit operator SampleId(Guid id) => new()
    {
        Value = id
    };

    public static implicit operator Guid(SampleId id) => id.Value;
}