using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Features.Tenants;

public static class TenantErrors
{
    public static Error DuplicatedCode => Error.Conflict("Tenant.DuplicatedCode", "Tenant with this code already exists");
    public static Error NameRequired => Error.Validation("Tenant.NameRequired", "Name is required");
} 