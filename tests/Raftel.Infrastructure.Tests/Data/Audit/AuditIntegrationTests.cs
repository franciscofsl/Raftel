using Microsoft.Extensions.DependencyInjection;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data.Audit;
using Raftel.Infrastructure.Data.Extensions;
using Raftel.Infrastructure.Data.Repositories.Audit;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Audit;

public class AuditIntegrationTests : InfrastructureTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        
        // Configure audit for Pirate entity
        services.AddAudit<TestingRaftelDbContext>(options =>
        {
            options.Add<Pirate>();
        });
    }

    [Fact]
    public async Task AuditInterceptor_ShouldCreateAuditEntry_WhenEntityIsCreated()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var pirateRepository = sp.GetRequiredService<IPirateRepository>();
            var auditRepository = sp.GetRequiredService<IAuditRepository>();

            // Create a new pirate
            var pirate = Pirate.Normal("Luffy", 3_000_000_000);
            await pirateRepository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            // Check audit entry was created
            var auditEntries = await auditRepository.GetEntityAuditHistoryAsync("Pirate", pirate.Id.Value.ToString());

            auditEntries.Count.ShouldBe(1);
            var auditEntry = auditEntries.First();
            auditEntry.ChangeType.ShouldBe(AuditChangeType.Create);
            auditEntry.EntityName.ShouldBe("Pirate");
            auditEntry.EntityId.ShouldBe(pirate.Id.Value.ToString());
            auditEntry.PropertyChanges.ShouldNotBeEmpty();
        });
    }

    [Fact]
    public async Task AuditInterceptor_ShouldCreateAuditEntry_WhenEntityIsUpdated()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var pirateRepository = sp.GetRequiredService<IPirateRepository>();
            var auditRepository = sp.GetRequiredService<IAuditRepository>();

            // Create a pirate
            var pirate = Pirate.Normal("Sanji", 77_000_000);
            await pirateRepository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            // Update the pirate
            pirate.Bounty = 150_000_000;
            pirateRepository.Update(pirate);
            await unitOfWork.CommitAsync();

            // Check audit entries
            var auditEntries = await auditRepository.GetEntityAuditHistoryAsync("Pirate", pirate.Id.Value.ToString());

            auditEntries.Count.ShouldBe(2);
            
            var createEntry = auditEntries.First(ae => ae.ChangeType == AuditChangeType.Create);
            createEntry.ShouldNotBeNull();

            var updateEntry = auditEntries.First(ae => ae.ChangeType == AuditChangeType.Update);
            updateEntry.ShouldNotBeNull();
            updateEntry.PropertyChanges.ShouldContain(pc => pc.PropertyName == "Bounty");
        });
    }

    [Fact]
    public async Task AuditInterceptor_ShouldCreateAuditEntry_WhenEntityIsDeleted()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var pirateRepository = sp.GetRequiredService<IPirateRepository>();
            var auditRepository = sp.GetRequiredService<IAuditRepository>();

            // Create a pirate
            var pirate = Pirate.Normal("Zoro", 320_000_000);
            await pirateRepository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            // Delete the pirate
            pirateRepository.Remove(pirate);
            await unitOfWork.CommitAsync();

            // Check audit entries
            var auditEntries = await auditRepository.GetEntityAuditHistoryAsync("Pirate", pirate.Id.Value.ToString());

            auditEntries.Count.ShouldBe(2);
            
            var createEntry = auditEntries.First(ae => ae.ChangeType == AuditChangeType.Create);
            createEntry.ShouldNotBeNull();

            var deleteEntry = auditEntries.First(ae => ae.ChangeType == AuditChangeType.Delete);
            deleteEntry.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task AuditRepository_ShouldReturnEmptyList_WhenEntityHasNoAuditHistory()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var auditRepository = sp.GetRequiredService<IAuditRepository>();

            var auditEntries = await auditRepository.GetEntityAuditHistoryAsync("NonExistentEntity", "123");

            auditEntries.ShouldBeEmpty();
        });
    }
}