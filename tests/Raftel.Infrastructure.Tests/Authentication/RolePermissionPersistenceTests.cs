using Microsoft.AspNetCore.Identity;
using Raftel.Domain.Features.Authorization;
using Raftel.Infrastructure.Authentication;

namespace Raftel.Infrastructure.Tests.Authentication;

public abstract class RolePermissionPersistenceTestsBase : InfrastructureTestBase
{
    protected RolePermissionPersistenceTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Role_ShouldReloadPermissionsItWasSavedWith_AndKeepAddedOnes()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var role = Role.Create("Admin").Value;
            role.AddPermissions(["A.One", "A.Two"]);
            await sp.GetRequiredService<IRolesRepository>().AddAsync(role);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var role = (await sp.GetRequiredService<IRolesRepository>().ListAllAsync()).Single();
            role.PermissionNames().OrderBy(n => n).ShouldBe(["A.One", "A.Two"]);

            role.AddPermission("A.Three");
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var role = (await sp.GetRequiredService<IRolesRepository>().ListAllAsync()).Single();
            role.PermissionNames().OrderBy(n => n).ShouldBe(["A.One", "A.Three", "A.Two"]);
        });
    }

    [Fact]
    public async Task Role_ShouldNotReloadARemovedPermission()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var role = Role.Create("Admin").Value;
            role.AddPermissions(["A.One", "A.Two"]);
            await sp.GetRequiredService<IRolesRepository>().AddAsync(role);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var role = (await sp.GetRequiredService<IRolesRepository>().ListAllAsync()).Single();
            role.RemovePermission("A.One").IsSuccess.ShouldBeTrue();
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var role = (await sp.GetRequiredService<IRolesRepository>().ListAllAsync()).Single();
            role.PermissionNames().ShouldBe(["A.Two"]);
        });
    }

    [Fact]
    public async Task Role_ShouldAllowGrantingAPermissionAgainAfterItWasRemoved()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var role = Role.Create("Admin").Value;
            role.AddPermission("A.One");
            await sp.GetRequiredService<IRolesRepository>().AddAsync(role);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var role = (await sp.GetRequiredService<IRolesRepository>().ListAllAsync()).Single();
            role.RemovePermission("A.One");
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var role = (await sp.GetRequiredService<IRolesRepository>().ListAllAsync()).Single();
            role.HasPermission("A.One").ShouldBeFalse();
            role.AddPermission("A.One");
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var role = (await sp.GetRequiredService<IRolesRepository>().ListAllAsync()).Single();
            role.PermissionNames().ShouldBe(["A.One"]);
        });
    }

    [Fact]
    public async Task TwoRoles_ShouldHoldTheSamePermissionNameIndependently()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IRolesRepository>();
            var first = Role.Create("First").Value;
            var second = Role.Create("Second").Value;
            first.AddPermission("Shared.Permission");
            second.AddPermission("Shared.Permission");
            await repository.AddAsync(first);
            await repository.AddAsync(second);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var roles = await sp.GetRequiredService<IRolesRepository>().ListAllAsync();
            roles.Count.ShouldBe(2);
            roles.ShouldAllBe(role => role.HasPermission("Shared.Permission"));
        });
    }

    [Fact]
    public async Task ClaimsPrincipalFactory_ShouldEmitTheStoredPermissionsOfTheUsersRoles()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var role = Role.Create("Admin").Value;
            role.AddPermissions(["A.One", "A.Two"]);
            await sp.GetRequiredService<IRolesRepository>().AddAsync(role);
            await sp.GetRequiredService<IUnitOfWork>().CommitAsync();

            (await sp.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole("Admin")))
                .Succeeded.ShouldBeTrue();

            var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser { UserName = "admin@example.com", Email = "admin@example.com" };
            (await userManager.CreateAsync(user, Guid.NewGuid().ToString("N") + "aA1!")).Succeeded.ShouldBeTrue();
            (await userManager.AddToRoleAsync(user, "Admin")).Succeeded.ShouldBeTrue();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var user = await sp.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync("admin@example.com");

            var principal = await sp.GetRequiredService<IClaimsPrincipalFactory>().CreateAsync(user!);

            principal.FindAll(RaftelClaimTypes.Permission).Select(c => c.Value).OrderBy(v => v)
                .ShouldBe(["A.One", "A.Two"]);
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerRolePermissionPersistenceTests : RolePermissionPersistenceTestsBase
{
    public SqlServerRolePermissionPersistenceTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlRolePermissionPersistenceTests : RolePermissionPersistenceTestsBase
{
    public PostgreSqlRolePermissionPersistenceTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
