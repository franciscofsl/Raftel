using Raftel.Domain.Features.Tenants;
using Shouldly;

namespace Raftel.Domain.Tests.Features.Tenants;

public class TenantTests
{
    [Fact]
    public void Create_ShouldRaiseTenantCreatedWithTheNewTenantId()
    {
        var tenant = Tenant.Create("Default", "DEFAULT", "The default tenant").Value;

        var raised = tenant.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TenantCreated>();
        raised.TenantId.ShouldBe(tenant.Id);
    }

    [Fact]
    public void Create_WithInvalidCode_ShouldRaiseNoEvent()
    {
        var result = Tenant.Create("Default", "", "The default tenant");

        result.IsFailure.ShouldBeTrue();
    }
}
