using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants.ValueObjects;

namespace Raftel.Domain.Features.Tenants;

public sealed record TenantCreated(TenantId TenantId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
