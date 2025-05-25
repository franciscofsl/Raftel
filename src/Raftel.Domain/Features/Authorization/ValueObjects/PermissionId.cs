using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Features.Authorization.ValueObjects;

public sealed record PermissionId : TypedGuidId
{
    public PermissionId(Guid value) : base(value)
    {
    }

    public static PermissionId New() => new(NewGuid());
} 