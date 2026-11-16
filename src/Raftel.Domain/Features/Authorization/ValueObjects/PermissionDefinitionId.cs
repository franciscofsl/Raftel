using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Features.Authorization.ValueObjects;

public sealed record PermissionDefinitionId : TypedGuidId
{
    public PermissionDefinitionId(Guid value) : base(value)
    {
    }

    public static PermissionDefinitionId New() => new(NewGuid());
}
