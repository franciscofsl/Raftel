using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.ValueObjects;

namespace Raftel.Infrastructure.Tests.Data.Features.Tenants;

public sealed class TenantsRepositoryTests : InfrastructureTestBase
{
    [Fact]
    public async Task CodeIsUniqueAsync_Should_ReturnTrue_WhenCodeDoesNotExist()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<ITenantsRepository>();

            var code = (Code)"TEST";
            var result = await repository.CodeIsUniqueAsync(code, CancellationToken.None);
            result.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task CodeIsUniqueAsync_Should_ReturnFalse_WhenCodeExists()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var repository = sp.GetRequiredService<ITenantsRepository>();

            var code = (Code)"TEST";
            var tenant = Tenant.Create("Test Tenant", "TEST", "Test Description").Value;
            await dbContext.Set<Tenant>().AddAsync(tenant);
            await dbContext.SaveChangesAsync();

            var result = await repository.CodeIsUniqueAsync(code, CancellationToken.None);
            result.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task CodeIsUniqueAsync_Should_ReturnTrue_WhenDifferentCase()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var repository = sp.GetRequiredService<ITenantsRepository>();

            var tenant = Tenant.Create("Test Tenant", "TEST", "Test Description").Value;
            await dbContext.Set<Tenant>().AddAsync(tenant);
            await dbContext.SaveChangesAsync();

            var newCode = (Code)"test1";
            var result = await repository.CodeIsUniqueAsync(newCode, CancellationToken.None);
            result.ShouldBeTrue();
        });
    }
}