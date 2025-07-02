using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Tenants;
using Shouldly;
using Testcontainers.MsSql;
using Xunit;

namespace Raftel.Domain.Tests.Features.Tenants;

public class TenantEntityFrameworkIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithPassword("yourStrong(!)Password")
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
    [Fact]
    public async Task Tenant_With_ConnectionString_Should_Persist_And_Retrieve_Correctly()
    {
        // Arrange
        const string connectionString = "Server=localhost;Database=MyTenantDb;User Id=sa;Password=mySecurePassword123!;";
        var tenant = Tenant.Create("Integration Test Tenant", "INTEGRATION_TENANT", "Test tenant for EF integration", connectionString).Value;

        var options = new DbContextOptionsBuilder<TestingRaftelDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        // Act & Assert - Save to database
        await using (var context = new TestingRaftelDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Tenant.Add(tenant);
            await context.SaveChangesAsync();
        }

        // Act & Assert - Retrieve from database
        await using (var context = new TestingRaftelDbContext(options))
        {
            var retrievedTenant = await context.Tenant.FirstOrDefaultAsync(t => t.Id == tenant.Id);

            retrievedTenant.ShouldNotBeNull();
            retrievedTenant.Name.ShouldBe("Integration Test Tenant");
            retrievedTenant.GetConnectionString().ShouldBe(connectionString);

            // Verify that the stored value is encrypted (different from plaintext)
            string storedEncryptedValue = retrievedTenant.ConnectionString;
            storedEncryptedValue.ShouldNotBe(connectionString);
            storedEncryptedValue.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Tenant_Without_ConnectionString_Should_Persist_And_Retrieve_Correctly()
    {
        // Arrange
        var tenant = Tenant.Create("Test Tenant", "TEST_TENANT", "Test tenant without connection string").Value;

        var options = new DbContextOptionsBuilder<TestingRaftelDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        // Act & Assert - Save to database
        await using (var context = new TestingRaftelDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Tenant.Add(tenant);
            await context.SaveChangesAsync();
        }

        // Act & Assert - Retrieve from database
        await using (var context = new TestingRaftelDbContext(options))
        {
            var retrievedTenant = await context.Tenant.FirstOrDefaultAsync(t => t.Id == tenant.Id);

            retrievedTenant.ShouldNotBeNull();
            retrievedTenant.Name.ShouldBe("Test Tenant");
            retrievedTenant.GetConnectionString().ShouldBeNull();
        }
    }

    [Fact]
    public async Task Multiple_Tenants_With_Different_ConnectionStrings_Should_Persist_Correctly()
    {
        // Arrange
        var tenant1 = Tenant.Create("Tenant 1", "TENANT_1", "First tenant", "Server=server1;Database=DB1;").Value;
        var tenant2 = Tenant.Create("Tenant 2", "TENANT_2", "Second tenant", "Server=server2;Database=DB2;").Value;
        var tenant3 = Tenant.Create("Tenant 3", "TENANT_3", "Third tenant").Value; // No connection string

        var options = new DbContextOptionsBuilder<TestingRaftelDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        // Act - Save to database
        await using (var context = new TestingRaftelDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Tenant.AddRange(tenant1, tenant2, tenant3);
            await context.SaveChangesAsync();
        }

        // Assert - Retrieve and verify all tenants
        await using (var context = new TestingRaftelDbContext(options))
        {
            var allTenants = await context.Tenant.ToListAsync();
            allTenants.Count.ShouldBe(3);

            var retrievedTenant1 = allTenants.First(t => t.Id == tenant1.Id);
            retrievedTenant1.GetConnectionString().ShouldBe("Server=server1;Database=DB1;");

            var retrievedTenant2 = allTenants.First(t => t.Id == tenant2.Id);
            retrievedTenant2.GetConnectionString().ShouldBe("Server=server2;Database=DB2;");

            var retrievedTenant3 = allTenants.First(t => t.Id == tenant3.Id);
            retrievedTenant3.GetConnectionString().ShouldBeNull();

            // Verify that each tenant's encrypted value is different
            string encrypted1 = retrievedTenant1.ConnectionString;
            string encrypted2 = retrievedTenant2.ConnectionString;
            
            encrypted1.ShouldNotBe(encrypted2);
            encrypted1.ShouldNotBe("Server=server1;Database=DB1;");
            encrypted2.ShouldNotBe("Server=server2;Database=DB2;");
        }
    }
}