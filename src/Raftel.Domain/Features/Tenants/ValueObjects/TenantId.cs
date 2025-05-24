using Raftel.Domain.BaseTypes;

namespace Raftel.Domain.Features.Tenants.ValueObjects;

public sealed record TenantId : TypedGuidId
{
    public TenantId(Guid value) : base(value)
    {
    }

    public static TenantId New() => new(NewGuid());
} 