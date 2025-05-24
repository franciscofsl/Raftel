using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Infrastructure.Multitenancy;

namespace Raftel.Infrastructure.Tests.Multitenancy;

public class CurrentTenantTests
{
    [Fact]
    public void Id_ShouldReturnNull_WhenNoTenantIsSet()
    {
        var currentTenant = new CurrentTenant();

        currentTenant.Id.ShouldBeNull();
    }

    [Fact]
    public void Change_ShouldSetTenantId_WhenCalled()
    {
        var currentTenant = new CurrentTenant();
        var tenantId = Guid.NewGuid();

        using var scope = currentTenant.Change(tenantId);

        currentTenant.Id.ShouldBe(tenantId);
    }

    [Fact]
    public void Change_ShouldRestorePreviousTenant_WhenScopeIsDisposed()
    {
        var currentTenant = new CurrentTenant();
        var originalTenantId = Guid.NewGuid();
        var temporaryTenantId = Guid.NewGuid();

        using (currentTenant.Change(originalTenantId))
        {
            currentTenant.Id.ShouldBe(originalTenantId);

            using (currentTenant.Change(temporaryTenantId))
            {
                currentTenant.Id.ShouldBe(temporaryTenantId);
            }

            currentTenant.Id.ShouldBe(originalTenantId);
        }

        currentTenant.Id.ShouldBeNull();
    }

    [Fact]
    public void Change_ShouldHandleNullTenantId_Correctly()
    {
        var currentTenant = new CurrentTenant();
        var tenantId = Guid.NewGuid();

        using (currentTenant.Change(tenantId))
        {
            currentTenant.Id.ShouldBe(tenantId);

            using (currentTenant.Change(null))
            {
                currentTenant.Id.ShouldBeNull();
            }

            currentTenant.Id.ShouldBe(tenantId);
        }

        currentTenant.Id.ShouldBeNull();
    }

    [Fact]
    public void Change_ShouldHandleNestedScopes_Correctly()
    {
        var currentTenant = new CurrentTenant();
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var tenant3 = Guid.NewGuid();

        currentTenant.Id.ShouldBeNull();

        using (currentTenant.Change(tenant1))
        {
            currentTenant.Id.ShouldBe(tenant1);

            using (currentTenant.Change(tenant2))
            {
                currentTenant.Id.ShouldBe(tenant2);

                using (currentTenant.Change(tenant3))
                {
                    currentTenant.Id.ShouldBe(tenant3);
                }

                currentTenant.Id.ShouldBe(tenant2);
            }

            currentTenant.Id.ShouldBe(tenant1);
        }

        currentTenant.Id.ShouldBeNull();
    }

    [Fact]
    public void Change_ShouldReturnDisposableScope_ThatRestoresPreviousState()
    {
        var currentTenant = new CurrentTenant();
        var tenantId = Guid.NewGuid();

        var scope = currentTenant.Change(tenantId);

        scope.ShouldNotBeNull();
        scope.ShouldBeAssignableTo<IDisposable>();
        currentTenant.Id.ShouldBe(tenantId);

        scope.Dispose();
        currentTenant.Id.ShouldBeNull();
    }

    [Fact]
    public async Task Change_ShouldBeThreadSafe_WithAsyncLocal()
    {
        var currentTenant = new CurrentTenant();
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var results = new List<Guid?>();
        var sync = new object();

        var task1 = Task.Run(async () =>
        {
            using (currentTenant.Change(tenant1))
            {
                await Task.Delay(50);
                lock (sync)
                {
                    results.Add(currentTenant.Id);
                }
            }
        });

        var task2 = Task.Run(async () =>
        {
            using (currentTenant.Change(tenant2))
            {
                await Task.Delay(50);
                lock (sync)
                {
                    results.Add(currentTenant.Id);
                }
            }
        });

        await Task.WhenAll(task1, task2);

        results.Count.ShouldBe(2);
        results.ShouldContain(tenant1);
        results.ShouldContain(tenant2);
        currentTenant.Id.ShouldBeNull();
    }

    [Fact]
    public void MultipleDispose_ShouldNotThrow()
    {
        var currentTenant = new CurrentTenant();
        var tenantId = Guid.NewGuid();
        var scope = currentTenant.Change(tenantId);

        scope.Dispose();
        Should.NotThrow(() => scope.Dispose());
        
        currentTenant.Id.ShouldBeNull();
    }

    [Fact]
    public void Change_WithSameTenantId_ShouldWork()
    {
        var currentTenant = new CurrentTenant();
        var tenantId = Guid.NewGuid();

        using (currentTenant.Change(tenantId))
        {
            currentTenant.Id.ShouldBe(tenantId);

            using (currentTenant.Change(tenantId))
            {
                currentTenant.Id.ShouldBe(tenantId);
            }

            currentTenant.Id.ShouldBe(tenantId);
        }

        currentTenant.Id.ShouldBeNull();
    }
} 