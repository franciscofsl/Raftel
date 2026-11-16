using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Authentication;

namespace Raftel.Infrastructure.Tests.Authentication;

/// <summary>
/// The token issued at login must carry the user's own tenant and the permissions of the roles of that
/// tenant, whatever tenant the login request itself runs in.
/// </summary>
public abstract class ClaimsPrincipalTenantTestsBase : InfrastructureTestBase
{
    private const string RoleName = "Manager";
    private const string TenantPermission = "Tenant.Permission";
    private const string HostPermission = "Host.Permission";

    protected ClaimsPrincipalTenantTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TenantUser_ShouldReceiveItsTenantClaim()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync("tenant@test.com", tenantId);

        var principal = await CreatePrincipalAsync("tenant@test.com", requestTenant: null);

        principal.FindFirst(RaftelClaimTypes.TenantId)!.Value.ShouldBe(tenantId.ToString());
    }

    [Fact]
    public async Task HostUser_ShouldNotReceiveATenantClaim()
    {
        await SeedUserAsync("host@test.com", tenantId: null);

        var principal = await CreatePrincipalAsync("host@test.com", requestTenant: Guid.NewGuid());

        principal.FindFirst(RaftelClaimTypes.TenantId).ShouldBeNull();
    }

    [Fact]
    public async Task IdentityUserWithoutDomainUser_ShouldNotReceiveATenantClaim()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var identityUser = new IdentityUser { UserName = "identity.only@test.com", Email = "identity.only@test.com" };
            (await sp.GetRequiredService<UserManager<IdentityUser>>().CreateAsync(identityUser, "Aa1!" + Guid.NewGuid()))
                .Succeeded.ShouldBeTrue();
        });

        var principal = await CreatePrincipalAsync("identity.only@test.com", requestTenant: null);

        principal.FindFirst(RaftelClaimTypes.TenantId).ShouldBeNull();
    }

    [Fact]
    public async Task SoftDeletedTenantUser_ShouldStillBePinnedToItsTenant()
    {
        var tenantId = Guid.NewGuid();
        await SeedUserAsync("deleted@test.com", tenantId);
        await ExecuteScopedAsync(async sp =>
        {
            using (sp.GetRequiredService<ICurrentTenant>().Change(tenantId))
            {
                var users = sp.GetRequiredService<IUsersRepository>();
                var user = (await users.ListAllAsync()).Single();
                users.Remove(user);
                await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
            }
        });

        var principal = await CreatePrincipalAsync("deleted@test.com", requestTenant: null);

        principal.FindFirst(RaftelClaimTypes.TenantId)!.Value.ShouldBe(tenantId.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenantUser_ShouldOnlyReceiveThePermissionsOfItsTenantsRoles(bool loginRunsInAnotherTenant)
    {
        var tenantId = Guid.NewGuid();
        await SeedRolesNamedManagerAsync(tenantId);
        await SeedUserAsync("tenant.manager@test.com", tenantId, RoleName);

        var principal = await CreatePrincipalAsync("tenant.manager@test.com",
            requestTenant: loginRunsInAnotherTenant ? Guid.NewGuid() : null);

        Permissions(principal).ShouldBe([TenantPermission]);
    }

    [Fact]
    public async Task HostUser_ShouldOnlyReceiveThePermissionsOfHostRolesEvenWhenLoginRunsInATenant()
    {
        var tenantId = Guid.NewGuid();
        await SeedRolesNamedManagerAsync(tenantId);
        await SeedUserAsync("host.manager@test.com", tenantId: null, RoleName);

        var principal = await CreatePrincipalAsync("host.manager@test.com", requestTenant: tenantId);

        Permissions(principal).ShouldBe([HostPermission]);
    }

    private static string[] Permissions(ClaimsPrincipal principal) =>
        principal.FindAll(RaftelClaimTypes.Permission).Select(c => c.Value).ToArray();

    private async Task SeedRolesNamedManagerAsync(Guid tenantId)
    {
        await ExecuteScopedAsync(async sp =>
        {
            var roles = sp.GetRequiredService<IRolesRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var hostRole = Role.Create(RoleName).Value;
            hostRole.AddPermission(HostPermission);
            await roles.AddAsync(hostRole);
            await unitOfWork.CommitAsync();

            using (sp.GetRequiredService<ICurrentTenant>().Change(tenantId))
            {
                var tenantRole = Role.Create(RoleName).Value;
                tenantRole.AddPermission(TenantPermission);
                await roles.AddAsync(tenantRole);
                await unitOfWork.CommitAsync();
            }

            (await sp.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole(RoleName)))
                .Succeeded.ShouldBeTrue();
        });
    }

    private async Task SeedUserAsync(string email, Guid? tenantId, string? identityRole = null)
    {
        await ExecuteScopedAsync(async sp =>
        {
            var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
            var identityUser = new IdentityUser { UserName = email, Email = email };
            (await userManager.CreateAsync(identityUser, "Aa1!" + Guid.NewGuid())).Succeeded.ShouldBeTrue();
            if (identityRole is not null)
            {
                (await userManager.AddToRoleAsync(identityUser, identityRole)).Succeeded.ShouldBeTrue();
            }

            using (sp.GetRequiredService<ICurrentTenant>().Change(tenantId))
            {
                var user = User.Create(email, "Name", "Surname");
                user.BindTo(identityUser.Id);
                await sp.GetRequiredService<IUsersRepository>().AddAsync(user);
                await sp.GetRequiredService<IUnitOfWork>().CommitAsync();
            }
        });
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(string email, Guid? requestTenant)
    {
        ClaimsPrincipal principal = null!;
        await ExecuteScopedAsync(async sp =>
        {
            var identityUser = await sp.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync(email);
            using (sp.GetRequiredService<ICurrentTenant>().Change(requestTenant))
            {
                principal = await sp.GetRequiredService<IClaimsPrincipalFactory>().CreateAsync(identityUser!);
            }
        });

        return principal;
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerClaimsPrincipalTenantTests : ClaimsPrincipalTenantTestsBase
{
    public SqlServerClaimsPrincipalTenantTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlClaimsPrincipalTenantTests : ClaimsPrincipalTenantTestsBase
{
    public PostgreSqlClaimsPrincipalTenantTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
