using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Infrastructure.Multitenancy;
using Shouldly;
using Xunit;

namespace Raftel.Application.IntegrationTests.Features.Tenants;

public class TenantSpecificDatabaseServiceTests
{
    [Fact]
    public async Task GetTenantDatabaseInfoAsync_WithTenantSpecificConnectionString_ShouldReturnTenantSpecificInfo()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantIdValueObject = new TenantId(tenantId);
        var tenantConnectionString = "Server=tenant-server;Database=TenantSpecificDB;User Id=tenant;Password=password;";
        
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var tenant = Tenant.Create("Test Tenant", "TEST_TENANT", "Test description", tenantConnectionString).Value;
        var tenantsRepository = Substitute.For<ITenantsRepository>();
        tenantsRepository.GetByIdAsync(tenantIdValueObject).Returns(tenant);

        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns("Server=default-server;Database=DefaultDB;User Id=default;Password=password;");

        var service = new TenantSpecificDatabaseService(currentTenant, tenantsRepository, configuration);

        // Act
        var result = await service.GetTenantDatabaseInfoAsync();

        // Assert
        result.TenantId.ShouldBe(tenantId);
        result.IsUsingTenantSpecificDatabase.ShouldBeTrue();
        result.DatabaseName.ShouldBe("TenantSpecificDB");
        result.ConnectionString.ShouldBe(tenantConnectionString);
    }

    [Fact]
    public async Task GetTenantDatabaseInfoAsync_WithoutTenantSpecificConnectionString_ShouldReturnDefaultInfo()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantIdValueObject = new TenantId(tenantId);
        var defaultConnectionString = "Server=default-server;Database=DefaultDB;User Id=default;Password=password;";
        
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        var tenant = Tenant.Create("Test Tenant", "TEST_TENANT", "Test description").Value; // No connection string
        var tenantsRepository = Substitute.For<ITenantsRepository>();
        tenantsRepository.GetByIdAsync(tenantIdValueObject).Returns(tenant);

        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns(defaultConnectionString);

        var service = new TenantSpecificDatabaseService(currentTenant, tenantsRepository, configuration);

        // Act
        var result = await service.GetTenantDatabaseInfoAsync();

        // Assert
        result.TenantId.ShouldBe(tenantId);
        result.IsUsingTenantSpecificDatabase.ShouldBeFalse();
        result.DatabaseName.ShouldBe("DefaultDB");
        result.ConnectionString.ShouldBe(defaultConnectionString);
    }

    [Fact]
    public async Task GetTenantDatabaseInfoAsync_WithoutCurrentTenant_ShouldReturnDefaultInfo()
    {
        // Arrange
        var defaultConnectionString = "Server=default-server;Database=DefaultDB;User Id=default;Password=password;";
        
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns((Guid?)null);

        var tenantsRepository = Substitute.For<ITenantsRepository>();
        var configuration = Substitute.For<IConfiguration>();
        configuration.GetConnectionString("Default").Returns(defaultConnectionString);

        var service = new TenantSpecificDatabaseService(currentTenant, tenantsRepository, configuration);

        // Act
        var result = await service.GetTenantDatabaseInfoAsync();

        // Assert
        result.TenantId.ShouldBeNull();
        result.IsUsingTenantSpecificDatabase.ShouldBeFalse();
        result.DatabaseName.ShouldBe("DefaultDB");
        result.ConnectionString.ShouldBe(defaultConnectionString);
    }
}