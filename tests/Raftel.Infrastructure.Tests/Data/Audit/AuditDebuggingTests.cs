using Microsoft.Extensions.DependencyInjection;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Audit;
using Raftel.Infrastructure.Data.Audit;
using Raftel.Infrastructure.Data.Extensions;
using Raftel.Infrastructure.Data.Interceptors;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Audit;

public class AuditDebuggingTests : InfrastructureTestBase
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
    public async Task Debug_AuditService_Registration()
    {
        await ExecuteScopedAsync(async sp =>
        {
            // Check if audit repository is registered
            var auditRepository = sp.GetService<IAuditRepository>();
            auditRepository.ShouldNotBeNull();

            // Check if audit interceptor is registered  
            var auditInterceptor = sp.GetService<AuditInterceptor>();
            auditInterceptor.ShouldNotBeNull();

            // Check if auditable entities options is registered
            var auditOptions = sp.GetService<AuditableEntitiesOptions>();
            auditOptions.ShouldNotBeNull();
            
            // Check if Pirate is configured for auditing
            var isPirateAuditable = auditOptions.IsAuditable(typeof(Pirate));
            isPirateAuditable.ShouldBeTrue();
            
            var pirateEntityName = auditOptions.GetEntityName(typeof(Pirate));
            pirateEntityName.ShouldBe("Pirate");

            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var pirateRepository = sp.GetRequiredService<IPirateRepository>();

            // Create a new pirate
            var pirate = Pirate.Normal("Luffy", 3_000_000_000);
            await pirateRepository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            // Try to get audit entries manually from the audit repository
            var auditEntries = await auditRepository.GetEntityAuditHistoryAsync("Pirate", ((Guid)pirate.Id).ToString());
            
            // Let's see what we get
            auditEntries.ShouldNotBeNull();
            // For debugging, let's just print the count without asserting
            System.Console.WriteLine($"Audit entries count: {auditEntries.Count}");
        });
    }
}