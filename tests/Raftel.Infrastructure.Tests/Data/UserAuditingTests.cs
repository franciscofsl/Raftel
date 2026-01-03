using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Data;

namespace Raftel.Infrastructure.Tests.Data;

public class UserAuditingTests : InfrastructureTestBase
{
    private static readonly Guid TestUserId = Guid.NewGuid();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Register a mock ICurrentUser
        services.AddScoped<ICurrentUser>(_ => new TestCurrentUser(TestUserId));
    }

    [Fact]
    public async Task AddAsync_ShouldSetCreatorIdAndCreationTime()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirate = Pirate.Normal("Luffy", 150_000_000);
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var entry = dbContext.Entry(pirate);
            var creatorId = entry.Property<Guid?>(ShadowPropertyNames.CreatorId).CurrentValue;
            var creationTime = entry.Property<DateTime?>(ShadowPropertyNames.CreationTime).CurrentValue;

            creatorId.ShouldBe(TestUserId);
            creationTime.ShouldNotBeNull();
            creationTime.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
        });
    }

    [Fact]
    public async Task Update_ShouldSetLastModifierIdAndLastModificationTime()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirate = Pirate.Normal("Zoro", 120_000_000);
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            // Modify the pirate
            pirate.Bounty = 200_000_000;
            repository.Update(pirate);
            await unitOfWork.CommitAsync();

            var entry = dbContext.Entry(pirate);
            var lastModifierId = entry.Property<Guid?>(ShadowPropertyNames.LastModifierId).CurrentValue;
            var lastModificationTime = entry.Property<DateTime?>(ShadowPropertyNames.LastModificationTime).CurrentValue;

            lastModifierId.ShouldBe(TestUserId);
            lastModificationTime.ShouldNotBeNull();
            lastModificationTime.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
        });
    }

    [Fact]
    public async Task AddAsync_WithoutCurrentUser_ShouldSetCreationTimeButNotCreatorId()
    {
        // Create a new test without ICurrentUser
        var services = new ServiceCollection();
        var fixture = new SqlServerTestContainerFixture();
        await fixture.InitializeAsync();

        try
        {
            services.AddSampleInfrastructure(fixture.ConnectionString);
            // Don't register ICurrentUser
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TestingRaftelDbContext>();
            await context.Database.EnsureCreatedAsync();

            var pirate = Pirate.Normal("Nami", 66_000_000);
            var repository = scope.ServiceProvider.GetRequiredService<IPirateRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var entry = context.Entry(pirate);
            var creatorId = entry.Property<Guid?>(ShadowPropertyNames.CreatorId).CurrentValue;
            var creationTime = entry.Property<DateTime?>(ShadowPropertyNames.CreationTime).CurrentValue;

            creatorId.ShouldBeNull();
            creationTime.ShouldNotBeNull();
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid userId)
        {
            UserId = userId;
        }

        public bool IsAuthenticated => true;
        public Guid? UserId { get; }
        public string? UserName => "TestUser";
        public IEnumerable<string> Roles => new List<string>();

        public void EnsureHasPermission(string permission)
        {
            // No-op for testing
        }
    }
}
