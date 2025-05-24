using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Validators;

namespace Raftel.Application.Features.Tenants.CreateTenant;

public sealed class CreateTenantCommandValidator : Validator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        EnsureThat(_ => !string.IsNullOrWhiteSpace(_.Name), TenantErrors.NameRequired);
    }
} 