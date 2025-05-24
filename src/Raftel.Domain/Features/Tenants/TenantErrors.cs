using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Tenants;

public static class TenantErrors
{
    public static Error DuplicatedCode => new("Tenant.DuplicatedCode", "Tenant with this code already exists");
    public static Error NameRequired => new("Tenant.NameRequired", "Name is required");
} 