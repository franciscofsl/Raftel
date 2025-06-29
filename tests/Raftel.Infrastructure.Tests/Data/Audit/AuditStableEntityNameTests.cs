using Microsoft.Extensions.DependencyInjection;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Audit;
using Raftel.Infrastructure.Data.Audit;
using Raftel.Infrastructure.Data.Extensions;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Audit;

public class AuditStableEntityNameTests : InfrastructureTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        
        // Configure audit for Pirate entity with a stable entity name
        services.AddAudit<TestingRaftelDbContext>(options =>
        {
            options.Add<Pirate>()
                .WithEntityName("StablePirateName"); // Use a stable name that won't change if class is renamed
        });
    }

    [Fact]
    public async Task AuditInterceptor_ShouldUseStableEntityName_WhenConfigured()
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

            // Check audit entry was created using the stable entity name
            var auditEntries = await auditRepository.GetEntityAuditHistoryAsync("StablePirateName", ((Guid)pirate.Id).ToString());

            auditEntries.Count.ShouldBe(1);
            var auditEntry = auditEntries.First();
            auditEntry.ChangeType.ShouldBe(AuditChangeType.Create);
            auditEntry.EntityName.ShouldBe("StablePirateName"); // Should use the configured stable name
            auditEntry.EntityId.ShouldBe(((Guid)pirate.Id).ToString());
            auditEntry.PropertyChanges.ShouldNotBeEmpty();

            // Verify that using the class name would NOT find the entries
            var auditEntriesWithClassName = await auditRepository.GetEntityAuditHistoryAsync("Pirate", ((Guid)pirate.Id).ToString());
            auditEntriesWithClassName.Count.ShouldBe(0); // Should be empty because we used a stable name
        });
    }

    [Fact]
    public async Task AuditableEntitiesOptions_GetEntityName_ShouldReturnStableName()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var auditOptions = sp.GetRequiredService<AuditableEntitiesOptions>();
            
            // The configured stable name should be returned instead of the type name
            var entityName = auditOptions.GetEntityName(typeof(Pirate));
            entityName.ShouldBe("StablePirateName");
            
            // But the type name would be "Pirate" if no stable name was configured
            var configuration = auditOptions.GetConfiguration(typeof(Pirate));
            configuration.ShouldNotBeNull();
            configuration.EntityType.Name.ShouldBe("Pirate");
        });
    }
}