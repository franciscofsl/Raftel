using Raftel.Domain.BaseTypes;

namespace Raftel.Infrastructure.Tests.Common.PiratesEntities;

public sealed record PirateId : TypedGuidId
{
    public PirateId(Guid value) : base(value)
    {
    }

    public static PirateId New() => new(Guid.NewGuid());
}