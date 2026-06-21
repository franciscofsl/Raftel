using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.Auditing;
using Raftel.Demo.Domain.Pirates;
using Raftel.Demo.Domain.Pirates.ValueObjects;
using Raftel.Demo.Domain.Ships;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Auditing;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Data.Auditing;
using Shouldly;

namespace Raftel.Infrastructure.Tests.Data.Auditing;

[Collection(SqlServerTestCollection.Name)]
public class EntityChangesTrackerInterceptorTests : InfrastructureTestBase
{
    private static readonly Guid TestUserId = Guid.NewGuid();
   
    public EntityChangesTrackerInterceptorTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
    
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddScoped<ICurrentUser>(_ => new TestCurrentUser(TestUserId));

        // Extends the demo's default audit configuration (Pirate, Ship) with User,
        // so the soft-delete scenario can be exercised too.
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

            var pirate = Pirate.Normal("Sanji", 330_000_000);
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
            entityChange.EntityFullName.ShouldBe(typeof(Pirate).FullName);
            entityChange.EntityId.ShouldBe(pirate.Id.ToString());

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

            var pirate = Pirate.Normal("Zoro", 120_000_000);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            pirate.Bounty = (Bounty)320_000_000u;
            repository.Update(pirate);
            await unitOfWork.CommitAsync();

            var auditLog = await dbContext.AuditLog
                .Include(a => a.EntityChanges)
                .ThenInclude(c => c.AffectedProperties)
                .Where(a => a.EntityChanges.Any(c => c.ChangeType == AuditChangeType.Updated))
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.ShouldNotBeNull();
            var entityChange = auditLog.EntityChanges.ShouldHaveSingleItem();
            entityChange.ChangeType.ShouldBe(AuditChangeType.Updated);

            var propertyChange = entityChange.AffectedProperties.First(_ => _.PropertyName == "Bounty");
            propertyChange.PropertyName.ShouldBe("Bounty");
            propertyChange.NewValue.ShouldBe(pirate.Bounty.ToString());
        });
    }

    [Fact]
    public async Task Delete_HardDelete_ShouldRecordDeletedEntityChange_WithOriginalValues()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = Pirate.Normal("Usopp", 200_000_000);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var originalBounty = pirate.Bounty.ToString();

            repository.Remove(pirate);
            await unitOfWork.CommitAsync();

            var auditLog = await dbContext.AuditLog
                .Include(a => a.EntityChanges)
                .ThenInclude(c => c.AffectedProperties)
                .Where(a => a.EntityChanges.Any(c => c.ChangeType == AuditChangeType.Deleted))
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.ShouldNotBeNull();
            var entityChange = auditLog.EntityChanges.ShouldHaveSingleItem();
            entityChange.ChangeType.ShouldBe(AuditChangeType.Deleted);

            var bountyChange = entityChange.AffectedProperties.Single(p => p.PropertyName == "Bounty");
            bountyChange.OldValue.ShouldBe(originalBounty);
            bountyChange.NewValue.ShouldBeNull();
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

            var user = User.Create("sanji@strawhat.crew", "Sanji", "Vinsmoke");
            user.BindTo("identity-user-id");
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

            // The user is only flagged via the IsDeleted shadow property (no business
            // property actually changes), so there is no property-level noise to record.
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

            var pirate = Pirate.Normal("Brook", 33_000_000);
            var ship = Ship.Create("Thousand Sunny");

            await pirateRepository.AddAsync(pirate);
            await shipRepository.AddAsync(ship);
            await unitOfWork.CommitAsync();

            var auditLogsCount = await dbContext.AuditLog.CountAsync();
            auditLogsCount.ShouldBe(1);

            var auditLog = await dbContext.AuditLog
                .Include(a => a.EntityChanges)
                .SingleAsync();

            var entityNames = auditLog.EntityChanges.Select(c => c.EntityFullName).ToList();
            entityNames.Count.ShouldBe(2);
            entityNames.ShouldContain(typeof(Pirate).FullName);
            entityNames.ShouldContain(typeof(Ship).FullName);
        });
    }

    [Fact]
    public async Task UnauditedEntityType_ShouldNotProduceAuditLog()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var rolesRepository = sp.GetRequiredService<IRolesRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var role = Role.Create("Quartermaster").Value;
            await rolesRepository.AddAsync(role);
            await unitOfWork.CommitAsync();

            var auditLogsCount = await dbContext.AuditLog.CountAsync();
            auditLogsCount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task AuditLogScope_WithActiveScope_ShouldCaptureCommandName()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();
            var auditLogScope = sp.GetRequiredService<IAuditLogScope>();

            var pirate = Pirate.Normal("Franky", 94_000_000);

            using (auditLogScope.Begin("CreatePirateCommand"))
            {
                await repository.AddAsync(pirate);
                await unitOfWork.CommitAsync();
            }

            var auditLog = await dbContext.AuditLog.OrderByDescending(a => a.Timestamp).FirstAsync();

            auditLog.Command.ShouldBe("CreatePirateCommand");
        });
    }

    [Fact]
    public async Task AuditLogScope_WithoutActiveScope_ShouldFallBackToUnknownCommand()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPirateRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var pirate = Pirate.Normal("Jinbe", 250_000_000);
            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var auditLog = await dbContext.AuditLog.OrderByDescending(a => a.Timestamp).FirstAsync();

            auditLog.Command.ShouldBe("Unknown");
        });
    }

    [Fact]
    public async Task Create_WithoutCurrentUser_ShouldRecordAuditLogWithNullUser()
    {
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

            var pirate = Pirate.Normal("Robin", 130_000_000);
            var repository = scope.ServiceProvider.GetRequiredService<IPirateRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await repository.AddAsync(pirate);
            await unitOfWork.CommitAsync();

            var auditLog = await context.AuditLog.FirstAsync();
            auditLog.UserId.ShouldBeNull();
            auditLog.UserName.ShouldBeNull();
        }
        finally
        {
            await fixture.DisposeAsync();
        }
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
