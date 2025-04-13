using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Tests.Common.Domain;

public sealed record PirateId : TypedGuidId
{
    public PirateId(Guid value) : base(value)
    {
    }

    public static PirateId New() => new(Guid.NewGuid());
}