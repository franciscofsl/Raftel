using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Features.Authorization.ValueObjects;

public sealed record RoleId : TypedGuidId
{
    public RoleId(Guid value) : base(value)
    {
    }

    public static RoleId New() => new(NewGuid());
} 