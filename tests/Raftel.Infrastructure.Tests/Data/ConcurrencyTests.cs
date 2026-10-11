using Microsoft.EntityFrameworkCore;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Application.Exceptions;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Data;

public abstract class ConcurrencyTestsBase : InfrastructureTestBase
{
    protected ConcurrencyTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task OptimisticConcurrencyEntities_HaveAVersionConcurrencyToken()
    {
        await ExecuteScopedAsync(sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();

            foreach (var clrType in new[] { typeof(Tenant), typeof(User), typeof(Role) })
            {
                var entityType = context.Model.FindEntityType(clrType)!;
                var version = entityType.FindProperty(ShadowPropertyNames.Version);

                version.ShouldNotBeNull();
                version.IsShadowProperty().ShouldBeTrue();
                version.IsConcurrencyToken.ShouldBeTrue();
                entityType.FindAnnotation(IEntityTypeConfigurationExtensions.OptimisticConcurrencyAnnotation)
                    .ShouldNotBeNull();

                if (context.Database.IsNpgsql())
                {
                    version.ClrType.ShouldBe(typeof(uint));
                    version.GetColumnName().ShouldBe("xmin");
                }
                else
                {
                    version.ClrType.ShouldBe(typeof(byte[]));
                }
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task EntitiesWithoutOptimisticConcurrency_HaveNoVersionProperty()
    {
        await ExecuteScopedAsync(sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();

            context.Model.FindEntityType(typeof(Pirate))!
                .FindProperty(ShadowPropertyNames.Version).ShouldBeNull();

            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task StaleWrite_OnTenant_Throws()
    {
        var tenant = await SeedAsync(Tenant.Create("Acme", "ACME", "Free").Value);

        await AssertStaleWriteThrowsAsync<Tenant>(
            tenant.Id,
            first => first.Description = "Pro",
            second => second.Name = "Acme Corp");

        await ExecuteScopedAsync(async sp =>
        {
            var loaded = await sp.GetRequiredService<TestingRaftelDbContext>().Set<Tenant>().FindAsync(tenant.Id);

            loaded!.Description.ShouldBe("Pro");
            loaded.Name.ShouldBe("Acme");
        });
    }

    [Fact]
    public async Task StaleWrite_OnUser_Throws()
    {
        var user = await SeedAsync(NewUser());

        await AssertStaleWriteThrowsAsync<User>(
            user.Id,
            first => first.Update("Monkey D.", "Luffy", "luffy@straw.hat"),
            second => second.Update("Roronoa", "Zoro", "zoro@straw.hat"));
    }

    [Fact]
    public async Task StaleWrite_OnRole_Throws()
    {
        var role = await SeedAsync(Role.Create("Captain", "Leads the crew").Value);

        await AssertStaleWriteThrowsAsync<Role>(
            role.Id,
            first => first.Description = "Leads",
            second => second.Rename("First mate"));
    }

    [Fact]
    public async Task StaleCommit_ThroughUnitOfWork_SurfacesConflict_NamingTheEntity()
    {
        var tenant = await SeedAsync(Tenant.Create("Acme", "ACME", "Free").Value);

        using var scopeA = ServiceProvider.CreateScope();
        using var scopeB = ServiceProvider.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var tenantA = (await contextA.Set<Tenant>().FindAsync(tenant.Id))!;
        var tenantB = (await contextB.Set<Tenant>().FindAsync(tenant.Id))!;

        tenantA.Description = "Pro";
        await scopeA.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();

        tenantB.Name = "Acme Corp";
        var conflict = await Should.ThrowAsync<ConcurrencyConflictException>(
            () => scopeB.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync());

        conflict.EntityNames.ShouldBe([nameof(Tenant)]);
        conflict.InnerException.ShouldBeOfType<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task StaleSynchronousSave_SurfacesConflict()
    {
        var tenant = await SeedAsync(Tenant.Create("Acme", "ACME", "Free").Value);

        using var scopeA = ServiceProvider.CreateScope();
        using var scopeB = ServiceProvider.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var tenantA = (await contextA.Set<Tenant>().FindAsync(tenant.Id))!;
        var tenantB = (await contextB.Set<Tenant>().FindAsync(tenant.Id))!;

        tenantA.Description = "Pro";
        contextA.SaveChanges();

        tenantB.Name = "Acme Corp";
        Should.Throw<ConcurrencyConflictException>(() => contextB.SaveChanges());
    }

    [Fact]
    public async Task NonConcurrencyFailure_PropagatesUnchanged()
    {
        await SeedAsync(Tenant.Create("Acme", "ACME", "Free").Value);

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            context.Add(Tenant.Create("Duplicate", "ACME", "Free").Value);

            var failure = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());

            failure.ShouldNotBeOfType<DbUpdateConcurrencyException>();
        });
    }

    [Fact]
    public async Task SequentialWrites_OnVersionTrackedEntity_Succeed()
    {
        var tenant = await SeedAsync(Tenant.Create("Acme", "ACME", "Free").Value);

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = (await context.Set<Tenant>().FindAsync(tenant.Id))!;
            loaded.Description = "Pro";
            await context.SaveChangesAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = (await context.Set<Tenant>().FindAsync(tenant.Id))!;
            loaded.Description = "Enterprise";
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task EntityWithoutOptimisticConcurrency_KeepsLastWriteWins()
    {
        var pirate = await SeedAsync(Pirate.Normal("Luffy", 100));

        using var scopeA = ServiceProvider.CreateScope();
        using var scopeB = ServiceProvider.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var pirateA = (await contextA.Set<Pirate>().FindAsync(pirate.Id))!;
        var pirateB = (await contextB.Set<Pirate>().FindAsync(pirate.Id))!;

        pirateA.Bounty = 200;
        await contextA.SaveChangesAsync();
        pirateB.Bounty = 300;
        await contextB.SaveChangesAsync();

        await ExecuteScopedAsync(async sp =>
        {
            var loaded = (await sp.GetRequiredService<TestingRaftelDbContext>().Set<Pirate>().FindAsync(pirate.Id))!;
            loaded.Bounty.ShouldBe((Bounty)300);
        });
    }

    [Fact]
    public async Task Version_ChangesAfterAnUpdate()
    {
        var tenant = await SeedAsync(Tenant.Create("Acme", "ACME", "Free").Value);

        var before = await ReadVersionAsync(tenant.Id);

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = (await context.Set<Tenant>().FindAsync(tenant.Id))!;
            loaded.Description = "Pro";
            await context.SaveChangesAsync();
        });

        var after = await ReadVersionAsync(tenant.Id);

        after.ShouldNotBe(before);
    }

    [Fact]
    public async Task StaleSoftDelete_OnVersionTrackedEntity_Throws()
    {
        var role = await SeedAsync(Role.Create("Captain", "Leads the crew").Value);

        using var scopeA = ServiceProvider.CreateScope();
        using var scopeB = ServiceProvider.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var roleA = (await contextA.Set<Role>().FindAsync(role.Id))!;
        var roleB = (await contextB.Set<Role>().FindAsync(role.Id))!;

        roleA.Description = "Leads";
        await contextA.SaveChangesAsync();

        contextB.Remove(roleB);

        await Should.ThrowAsync<ConcurrencyConflictException>(() => contextB.SaveChangesAsync());
    }

    [Fact]
    public async Task TenantFilter_StillApplies_ToVersionTrackedEntity()
    {
        var tenantOne = Guid.NewGuid();
        var tenantTwo = Guid.NewGuid();
        User user = null!;

        await ExecuteScopedAsync(async sp =>
        {
            using var _ = sp.GetRequiredService<ICurrentTenant>().Change(tenantOne);
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            user = NewUser();
            context.Add(user);
            await context.SaveChangesAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            using (currentTenant.Change(tenantTwo))
            {
                (await context.Set<User>().AnyAsync(u => u.Id == user.Id)).ShouldBeFalse();
            }

            using (currentTenant.Change(tenantOne))
            {
                (await context.Set<User>().AnyAsync(u => u.Id == user.Id)).ShouldBeTrue();
            }
        });
    }

    private static User NewUser()
    {
        var user = User.Create(Guid.NewGuid() + "@straw.hat", "Monkey D.", "Luffy");
        user.BindTo(Guid.NewGuid().ToString());
        return user;
    }

    private async Task<TEntity> SeedAsync<TEntity>(TEntity entity) where TEntity : class
    {
        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            context.Add(entity);
            await context.SaveChangesAsync();
        });

        return entity;
    }

    private async Task AssertStaleWriteThrowsAsync<TEntity>(
        object id, Action<TEntity> first, Action<TEntity> second) where TEntity : class
    {
        using var scopeA = ServiceProvider.CreateScope();
        using var scopeB = ServiceProvider.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();

        var entityA = (await contextA.Set<TEntity>().FindAsync(id))!;
        var entityB = (await contextB.Set<TEntity>().FindAsync(id))!;

        first(entityA);
        await contextA.SaveChangesAsync();

        second(entityB);
        await Should.ThrowAsync<ConcurrencyConflictException>(() => contextB.SaveChangesAsync());
    }

    private async Task<string> ReadVersionAsync(TenantId id)
    {
        var version = string.Empty;

        await ExecuteScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<TestingRaftelDbContext>();
            var loaded = (await context.Set<Tenant>().FindAsync(id))!;
            var raw = context.Entry(loaded).Property(ShadowPropertyNames.Version).CurrentValue;
            version = raw is byte[] bytes ? Convert.ToBase64String(bytes) : raw!.ToString()!;
        });

        return version;
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerConcurrencyTests : ConcurrencyTestsBase
{
    public SqlServerConcurrencyTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlConcurrencyTests : ConcurrencyTestsBase
{
    public PostgreSqlConcurrencyTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
