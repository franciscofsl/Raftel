using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Raftel.Application;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;

namespace Raftel.Infrastructure.Seeding.Seeders;

/// <summary>
/// Creates the configured default roles (and their Identity counterparts) and grants them the
/// configured permissions. Existing roles only gain missing permissions; nothing is ever removed.
/// </summary>
internal sealed class DefaultRolesSeeder(
    IOptions<SeedOptions> options,
    IRolesRepository rolesRepository,
    IPermissionDefinitionsRepository definitionsRepository,
    RoleManager<IdentityRole> roleManager) : IDataSeeder
{
    public int Order => 200;

    public string Name => "default-roles";

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await definitionsRepository.ListAllAsync(cancellationToken: cancellationToken);
        var known = catalog.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var active = catalog.Where(d => !d.IsObsolete).Select(d => d.Name).ToList();

        foreach (var configured in ConfiguredRoles())
        {
            var permissions = Resolve(configured, known, active);
            if (permissions.IsFailure)
            {
                return Result.Failure(permissions.Error);
            }

            var role = await FindOrCreateRoleAsync(configured, cancellationToken);
            if (role.IsFailure)
            {
                return Result.Failure(role.Error);
            }

            foreach (var permission in permissions.Value.Where(p => !role.Value.HasPermission(p)))
            {
                var added = role.Value.AddPermission(permission);
                if (added.IsFailure)
                {
                    return added;
                }
            }

            var identityRole = await EnsureIdentityRoleAsync(role.Value.Name);
            if (identityRole.IsFailure)
            {
                return identityRole;
            }
        }

        return Result.Success();
    }

    private IReadOnlyList<SeedRoleOptions> ConfiguredRoles()
    {
        var seed = options.Value;
        if (seed.Roles.Count > 0)
        {
            return seed.Roles;
        }

        return
        [
            new SeedRoleOptions
            {
                Name = seed.AdminRoleName,
                Description = "Administrator role with full access to the system",
                Permissions = [SeedRoleOptions.AllPermissions]
            }
        ];
    }

    private static Result<IReadOnlyList<string>> Resolve(
        SeedRoleOptions configured,
        HashSet<string> known,
        List<string> active)
    {
        var resolved = new List<string>();
        foreach (var name in configured.Permissions)
        {
            if (name == SeedRoleOptions.AllPermissions)
            {
                resolved.AddRange(active);
                continue;
            }

            if (!known.Contains(name))
            {
                return Result.Failure<IReadOnlyList<string>>(Error.NotFound(
                    "Seeding.UnknownPermission",
                    $"Role '{configured.Name}' is configured with permission '{name}', which is not in the permission catalog."));
            }

            resolved.Add(name);
        }

        IReadOnlyList<string> distinct = resolved.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return Result.Success(distinct);
    }

    private async Task<Result<Role>> FindOrCreateRoleAsync(SeedRoleOptions configured, CancellationToken cancellationToken)
    {
        var lowered = configured.Name.ToLower();
        var existing = await rolesRepository.ListAllAsync(r => r.Name.ToLower() == lowered, cancellationToken);
        if (existing.Count > 0)
        {
            return Result.Success(existing[0]);
        }

        var created = Role.Create(configured.Name, configured.Description);
        if (created.IsFailure)
        {
            return created;
        }

        await rolesRepository.AddAsync(created.Value, cancellationToken);
        return created;
    }

    private async Task<Result> EnsureIdentityRoleAsync(string roleName)
    {
        if (await roleManager.FindByNameAsync(roleName) is not null)
        {
            return Result.Success();
        }

        var created = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (created.Succeeded)
        {
            return Result.Success();
        }

        var message = created.Errors.Select(e => e.Description).FirstOrDefault() ?? "Identity rejected the role.";
        return Result.Failure(Error.Failure("Seeding.IdentityRoleFailed", $"Could not create identity role '{roleName}': {message}"));
    }
}
