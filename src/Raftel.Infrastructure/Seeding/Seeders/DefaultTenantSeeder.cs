using Microsoft.Extensions.Options;
using Raftel.Application;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.ValueObjects;

namespace Raftel.Infrastructure.Seeding.Seeders;

/// <summary>
/// Creates the configured default tenant when no tenant with its code exists.
/// </summary>
internal sealed class DefaultTenantSeeder(
    IOptions<SeedOptions> options,
    ITenantsRepository tenantsRepository) : IDataSeeder
{
    public int Order => 50;

    public string Name => "default-tenant";

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value.DefaultTenant;
        if (configured is null || string.IsNullOrWhiteSpace(configured.Code))
        {
            return Result.Success();
        }

        var code = Code.Create(configured.Code);
        if (code.IsFailure)
        {
            return Result.Failure(code.Error);
        }

        if (!await tenantsRepository.CodeIsUniqueAsync(code.Value, cancellationToken))
        {
            return Result.Success();
        }

        var tenant = Tenant.Create(configured.Name, configured.Code, configured.Description);
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        await tenantsRepository.AddAsync(tenant.Value, cancellationToken);
        return Result.Success();
    }
}
