using Microsoft.Extensions.Logging;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Application.Authorization;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;

namespace Raftel.Infrastructure.Seeding.Seeders;

/// <summary>
/// Reconciles the permission catalog with the permission names declared in code. Permissions that
/// disappear from code are flagged obsolete, never deleted, because roles may still hold them.
/// </summary>
internal sealed class PermissionsSeeder(
    IPermissionSetProvider permissionSetProvider,
    IPermissionDefinitionsRepository definitionsRepository,
    ILogger<PermissionsSeeder> logger) : IDataSeeder
{
    public int Order => 100;

    public string Name => "permissions";

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        var declared = permissionSetProvider.PermissionNames;
        var existing = await definitionsRepository.ListAllAsync(cancellationToken: cancellationToken);
        var existingByName = existing.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in declared)
        {
            if (existingByName.TryGetValue(name, out var definition))
            {
                definition.Reinstate();
                continue;
            }

            var created = PermissionDefinition.Create(name);
            if (created.IsFailure)
            {
                return Result.Failure(created.Error);
            }

            await definitionsRepository.AddAsync(created.Value, cancellationToken);
        }

        var declaredNames = new HashSet<string>(declared, StringComparer.OrdinalIgnoreCase);
        foreach (var definition in existing.Where(d => !declaredNames.Contains(d.Name)))
        {
            if (definition.MarkObsolete())
            {
                logger.LogWarning(
                    "Permission {Permission} is no longer declared in code and was flagged obsolete; roles that hold it keep it.",
                    definition.Name);
            }
        }

        return Result.Success();
    }
}
