using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Auditing;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Data.Auditing;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Auditing;

[Collection(PostgreSqlTestCollection.Name)]
public class EntityChangesTrackerInterceptorPostgreSqlTests : PostgreSqlInfrastructureTestBase
{
    private static readonly Guid TestUserId = Guid.NewGuid();
   
    public EntityChangesTrackerInterceptorPostgreSqlTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
    
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddScoped<ICurrentUser>(_ => new TestCurrentUser(TestUserId));
        services.AddSingleton(new AuditOptions().Audit<Pirate>().Audit<Ship>().Audit<User>());
    }

    [Fact]
    public async Task Create_ShouldRecordAuditLog_WithCreatedEntityChange_AndPropertyChanges()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = Pirate.Normal("Chopper", 1_050);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var auditLog = await dbContext.AuditLog
                .Include(a => a.EntityChanges)
                .ThenInclude(c => c.AffectedProperties)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.ShouldNotBeNull();
            auditLog.UserId.ShouldBe(TestUserId);

            var entityChange = auditLog.EntityChanges.ShouldHaveSingleItem();
            entityChange.ChangeType.ShouldBe(AuditChangeType.Created);

            var bountyChange = entityChange.AffectedProperties.Single(p => p.PropertyName == "Bounty");
            bountyChange.OldValue.ShouldBeNull();
            bountyChange.NewValue.ShouldBe(pirate.Bounty.ToString());
        });
    }

    [Fact]
    public async Task Update_ShouldRecordOnlyTheModifiedProperty()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = Pirate.Normal("Nico Robin", 130_000_000);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            pirate.Bounty = (Bounty)260_000_000u;
            repository.Update(pirate);
            await unitOfWork.CommitAsync();

            var auditLog = await dbContext.AuditLog
                .Include(a => a.EntityChanges)
                .ThenInclude(c => c.AffectedProperties)
                .Where(a => a.EntityChanges.Any(c => c.ChangeType == AuditChangeType.Updated))
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.ShouldNotBeNull();
            var propertyChange = auditLog.EntityChanges.ShouldHaveSingleItem().AffectedProperties.ShouldHaveSingleItem();
            propertyChange.PropertyName.ShouldBe("Bounty");
            propertyChange.NewValue.ShouldBe(pirate.Bounty.ToString());
        });
    }

    [Fact]
    public async Task Delete_SoftDelete_ShouldRecordDeletedEntityChange_WithoutPropertyNoise()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IUsersRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var user = User.Create("usopp@strawhat.crew", "Usopp", "Sniper");
            await repository.AddAsync(user);
            await unitOfWork.CommitAsync();

            repository.Remove(user);
            await unitOfWork.CommitAsync();

            var auditLog = await dbContext.AuditLog
                .Include(a => a.EntityChanges)
                .ThenInclude(c => c.AffectedProperties)
                .Where(a => a.EntityChanges.Any(c =>
                    c.EntityFullName == typeof(User).FullName && c.ChangeType == AuditChangeType.Deleted))
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.ShouldNotBeNull();
            var entityChange = auditLog.EntityChanges.ShouldHaveSingleItem();
            entityChange.ChangeType.ShouldBe(AuditChangeType.Deleted);
            entityChange.AffectedProperties.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task MultipleEntities_ChangedInSameTransaction_ShouldProduceOneAuditLog_WithOneEntityChangePerEntity()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var pirateRepository = sp.GetRequiredService<IPirateRepository>();
            var shipRepository = sp.GetRequiredService<IShipRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = Pirate.Normal("Carrot", 18_000);
            var ship = Ship.Create("Going Merry");

            await pirateRepository.AddAsync(pirate);
            await shipRepository.AddAsync(ship);
            await unitOfWork.CommitAsync();

            var auditLogsCount = await dbContext.AuditLog.CountAsync();
            auditLogsCount.ShouldBe(1);

            var auditLog = await dbContext.AuditLog.Include(a => a.EntityChanges).SingleAsync();

            var entityNames = auditLog.EntityChanges.Select(c => c.EntityFullName).ToList();
            entityNames.Count.ShouldBe(2);
            entityNames.ShouldContain(typeof(Pirate).FullName);
            entityNames.ShouldContain(typeof(Ship).FullName);
        });
    }

    private sealed class TestCurrentUser : ICurrentUser
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
