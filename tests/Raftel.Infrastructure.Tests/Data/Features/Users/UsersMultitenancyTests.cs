using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Users;
using Raftel.Domain.Features.Users.ValueObjects;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Infrastructure.Tests.Data.Features.Users;

public class UsersMultitenancyTests : InfrastructureTestBase
{
    private static User CreateUser(string email, string name, string surname)
    {
        var user = User.Create(email, name, surname);
        user.BindTo(Guid.NewGuid().ToString());
        return user;
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOnlyCurrentTenantUser_WhenTenantIsSet()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var whitebeardCrewId = Guid.NewGuid();

            var luffy = CreateUser("luffy@strawhat.crew", "Monkey D.", "Luffy");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            var whitebeard = CreateUser("whitebeard@whitebeard.crew", "Edward", "Newgate");

            using (currentTenant.Change(whitebeardCrewId))
            {
                await repository.AddAsync(whitebeard);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(strawHatCrewId))
            {
                var userFromStrawHat = await repository.GetByIdAsync(luffy.Id);
                userFromStrawHat.ShouldNotBeNull();
                userFromStrawHat.ShouldBe(luffy);

                var userFromWhitebeard = await repository.GetByIdAsync(whitebeard.Id);
                userFromWhitebeard.ShouldBeNull();
            }

            using (currentTenant.Change(whitebeardCrewId))
            {
                var userFromStrawHat = await repository.GetByIdAsync(luffy.Id);
                userFromStrawHat.ShouldBeNull();

                var userFromWhitebeard = await repository.GetByIdAsync(whitebeard.Id);
                userFromWhitebeard.ShouldNotBeNull();
                userFromWhitebeard.ShouldBe(whitebeard);
            }
        });
    }

    [Fact]
    public async Task ListAllAsync_ShouldReturnOnlyCurrentTenantUsers_WhenTenantIsSet()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var redHairCrewId = Guid.NewGuid();

            var luffy = CreateUser("luffy@strawhat.crew", "Monkey D.", "Luffy");
            var zoro = CreateUser("zoro@strawhat.crew", "Roronoa", "Zoro");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await repository.AddAsync(zoro);
                await unitOfWork.CommitAsync();
            }

            var shanks = CreateUser("shanks@redhair.crew", "Red-Haired", "Shanks");
            var beckman = CreateUser("beckman@redhair.crew", "Benn", "Beckman");

            using (currentTenant.Change(redHairCrewId))
            {
                await repository.AddAsync(shanks);
                await repository.AddAsync(beckman);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(strawHatCrewId))
            {
                var strawHatUsers = await repository.ListAllAsync();
                strawHatUsers.Count.ShouldBe(2);
                strawHatUsers.ShouldContain(luffy);
                strawHatUsers.ShouldContain(zoro);
                strawHatUsers.ShouldNotContain(shanks);
                strawHatUsers.ShouldNotContain(beckman);
            }

            using (currentTenant.Change(redHairCrewId))
            {
                var redHairUsers = await repository.ListAllAsync();
                redHairUsers.Count.ShouldBe(2);
                redHairUsers.ShouldContain(shanks);
                redHairUsers.ShouldContain(beckman);
                redHairUsers.ShouldNotContain(luffy);
                redHairUsers.ShouldNotContain(zoro);
            }
        });
    }

    [Fact]
    public async Task ListAllAsync_ShouldReturnAllUsers_WhenNoTenantIsSet()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var bigMomCrewId = Guid.NewGuid();

            var luffy = CreateUser("luffy@strawhat.crew", "Monkey D.", "Luffy");
            var bigMom = CreateUser("bigmom@bigmom.crew", "Charlotte", "Linlin");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(bigMomCrewId))
            {
                await repository.AddAsync(bigMom);
                await unitOfWork.CommitAsync();
            }

            var allUsers = await repository.ListAllAsync();
            allUsers.Count.ShouldBe(2);
            allUsers.ShouldContain(luffy);
            allUsers.ShouldContain(bigMom);
        });
    }

    [Fact]
    public async Task AddAsync_ShouldSetTenantId_WhenTenantIsActive()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var beastPiratesId = Guid.NewGuid();
            var kaido = CreateUser("kaido@beast.pirates", "Kaido", "King of Beasts");

            using (currentTenant.Change(beastPiratesId))
            {
                await repository.AddAsync(kaido);
                await unitOfWork.CommitAsync();

                var entry = dbContext.Entry(kaido);
                entry.Property(ShadowPropertyNames.TenantId).CurrentValue.ShouldBe(beastPiratesId);
            }
        });
    }

    [Fact]
    public async Task AddAsync_ShouldNotSetTenantId_WhenNoTenantIsActive()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var garp = CreateUser("garp@marine.hq", "Monkey D.", "Garp");

            await repository.AddAsync(garp);
            await unitOfWork.CommitAsync();

            var entry = dbContext.Entry(garp);
            entry.Property(ShadowPropertyNames.TenantId).CurrentValue.ShouldBeNull();
        });
    }

    [Fact]
    public async Task EmailIsUniqueAsync_ShouldRespectTenantBoundaries()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var blackbeardCrewId = Guid.NewGuid();
            var email = "captain@pirate.crew";

            var luffy = CreateUser(email, "Monkey D.", "Luffy");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();

                var isUniqueInStrawHat = await repository.EmailIsUniqueAsync(email, CancellationToken.None);
                isUniqueInStrawHat.ShouldBeFalse();
            }

            using (currentTenant.Change(blackbeardCrewId))
            {
                var isUniqueInBlackbeard = await repository.EmailIsUniqueAsync(email, CancellationToken.None);
                isUniqueInBlackbeard.ShouldBeTrue();

                var blackbeard = CreateUser(email, "Marshall D.", "Teach");
                await repository.AddAsync(blackbeard);
                await unitOfWork.CommitAsync();

                var isUniqueAfterAdd = await repository.EmailIsUniqueAsync(email, CancellationToken.None);
                isUniqueAfterAdd.ShouldBeFalse();
            }
        });
    }

    [Fact]
    public async Task ListAllAsync_ShouldReturnAllUsers_WhenTenantFilterIsDisabled()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();

            var strawHatCrewId = Guid.NewGuid();
            var heartPiratesId = Guid.NewGuid();

            var luffy = CreateUser("luffy@strawhat.crew", "Monkey D.", "Luffy");
            var law = CreateUser("law@heart.pirates", "Trafalgar D. Water", "Law");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(heartPiratesId))
            {
                await repository.AddAsync(law);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(strawHatCrewId))
            {
                using (dataFilter.Disable<ITenantFilter>())
                {
                    var allUsers = await repository.ListAllAsync();
                    allUsers.Count.ShouldBe(2);
                    allUsers.ShouldContain(luffy);
                    allUsers.ShouldContain(law);
                }
            }
        });
    }

    [Fact]
    public async Task AddAsync_ShouldAllowSameEmailInDifferentTenants()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var kidPiratesId = Guid.NewGuid();
            var captainEmail = "captain@pirate.crew";

            var luffy = CreateUser(captainEmail, "Monkey D.", "Luffy");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            var kid = CreateUser(captainEmail, "Eustass", "Kid");

            using (currentTenant.Change(kidPiratesId))
            {
                await repository.AddAsync(kid);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(strawHatCrewId))
            {
                var usersInStrawHat = await repository.ListAllAsync();
                usersInStrawHat.Count.ShouldBe(1);
                usersInStrawHat.First().Email.ToString().ShouldBe(captainEmail);
                usersInStrawHat.First().Name.ShouldBe("Monkey D.");
            }

            using (currentTenant.Change(kidPiratesId))
            {
                var usersInKidPirates = await repository.ListAllAsync();
                usersInKidPirates.Count.ShouldBe(1);
                usersInKidPirates.First().Email.ToString().ShouldBe(captainEmail);
                usersInKidPirates.First().Name.ShouldBe("Eustass");
            }
        });
    }

    [Fact]
    public async Task EmailIsUniqueAsync_ShouldReturnTrue_ForSameEmailInDifferentTenant()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var donquixoteId = Guid.NewGuid();
            var email = "captain@grandline.sea";

            var luffy = CreateUser(email, "Monkey D.", "Luffy");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();

                var isUniqueInStrawHat = await repository.EmailIsUniqueAsync(email, CancellationToken.None);
                isUniqueInStrawHat.ShouldBeFalse();
            }

            using (currentTenant.Change(donquixoteId))
            {
                var isUniqueInDonquixote = await repository.EmailIsUniqueAsync(email, CancellationToken.None);
                isUniqueInDonquixote.ShouldBeTrue();
            }
        });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldNotFindUserFromDifferentTenant_EvenWithSameEmail()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var rogerPiratesId = Guid.NewGuid();
            var kingEmail = "pirate.king@grandline.sea";

            var luffy = CreateUser(kingEmail, "Monkey D.", "Luffy");
            var roger = CreateUser(kingEmail, "Gol D.", "Roger");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(luffy);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(rogerPiratesId))
            {
                await repository.AddAsync(roger);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(strawHatCrewId))
            {
                var foundLuffy = await repository.GetByIdAsync(luffy.Id);
                foundLuffy.ShouldNotBeNull();
                foundLuffy.Name.ShouldBe("Monkey D.");

                var foundRoger = await repository.GetByIdAsync(roger.Id);
                foundRoger.ShouldBeNull();
            }

            using (currentTenant.Change(rogerPiratesId))
            {
                var foundLuffy = await repository.GetByIdAsync(luffy.Id);
                foundLuffy.ShouldBeNull();

                var foundRoger = await repository.GetByIdAsync(roger.Id);
                foundRoger.ShouldNotBeNull();
                foundRoger.Name.ShouldBe("Gol D.");
            }
        });
    }

    [Fact]
    public async Task ListAllAsync_ShouldShowOnlyTenantUsers_EvenWithDuplicateEmails()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var whitebeardCrewId = Guid.NewGuid();
            var redHairCrewId = Guid.NewGuid();
            var admiralEmail = "admiral@marine.hq";

            var aokiji = CreateUser(admiralEmail, "Kuzan", "Aokiji");
            var akainu = CreateUser(admiralEmail, "Sakazuki", "Akainu");
            var kizaru = CreateUser(admiralEmail, "Borsalino", "Kizaru");

            using (currentTenant.Change(strawHatCrewId))
            {
                await repository.AddAsync(aokiji);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(whitebeardCrewId))
            {
                await repository.AddAsync(akainu);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(redHairCrewId))
            {
                await repository.AddAsync(kizaru);
                await unitOfWork.CommitAsync();
            }

            using (currentTenant.Change(strawHatCrewId))
            {
                var users = await repository.ListAllAsync();
                users.Count.ShouldBe(1);
                users.First().Surname.ShouldBe("Aokiji");
            }

            using (currentTenant.Change(whitebeardCrewId))
            {
                var users = await repository.ListAllAsync();
                users.Count.ShouldBe(1);
                users.First().Surname.ShouldBe("Akainu");
            }

            using (currentTenant.Change(redHairCrewId))
            {
                var users = await repository.ListAllAsync();
                users.Count.ShouldBe(1);
                users.First().Surname.ShouldBe("Kizaru");
            }
        });
    }

    [Fact]
    public async Task EmailIsUniqueAsync_ShouldPreventDuplicatesWithinSameTenant()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var currentTenant = sp.GetRequiredService<ICurrentTenant>();

            var strawHatCrewId = Guid.NewGuid();
            var email = "navigator@strawhat.crew";

            using (currentTenant.Change(strawHatCrewId))
            {
                var isUniqueInitially = await repository.EmailIsUniqueAsync(email, CancellationToken.None);
                isUniqueInitially.ShouldBeTrue();

                var nami = CreateUser(email, "Nami", "Cat Burglar");
                await repository.AddAsync(nami);
                await unitOfWork.CommitAsync();

                var isUniqueAfterAdd = await repository.EmailIsUniqueAsync(email, CancellationToken.None);
                isUniqueAfterAdd.ShouldBeFalse();
            }
        });
    }
} 