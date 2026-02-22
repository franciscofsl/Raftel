using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;

namespace Raftel.Infrastructure.Tests.Data.Features.Users;

public class UsersSoftDeleteTests : InfrastructureTestBase
{
    private static User CreateUser(string email, string name, string surname)
    {
        var user = User.Create(email, name, surname);
        user.BindTo(Guid.NewGuid().ToString());
        return user;
    }

    [Fact]
    public async Task Remove_ShouldExcludeUser_FromNormalQueries()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();

            var user = CreateUser("ace@whitebeard.crew", "Portgas D.", "Ace");
            await repository.AddAsync(user);
            await unitOfWork.CommitAsync();

            repository.Remove(user);
            await unitOfWork.CommitAsync();

            var deleted = await repository.GetByIdAsync(user.Id);
            deleted.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Remove_ShouldSetIsDeletedTrue_OnUser()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var user = CreateUser("sabo@revolutionary.army", "Sabo", "Chief of Staff");
            await repository.AddAsync(user);
            await unitOfWork.CommitAsync();

            repository.Remove(user);
            await unitOfWork.CommitAsync();

            var entry = dbContext.Entry(user);
            entry.Property(ShadowPropertyNames.IsDeleted).CurrentValue.ShouldBe(true);
        });
    }

    [Fact]
    public async Task Remove_ShouldStillReturnUser_WhenSoftDeleteFilterIsDisabled()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IUsersRepository>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();

            var user = CreateUser("robin@strawhat.crew", "Nico", "Robin");
            await repository.AddAsync(user);
            await unitOfWork.CommitAsync();

            repository.Remove(user);
            await unitOfWork.CommitAsync();

            var deleted = await repository.GetByIdAsync(user.Id);
            deleted.ShouldBeNull();

            using (dataFilter.Disable<ISoftDeleteFilter>())
            {
                var softDeleted = await repository.GetByIdAsync(user.Id);
                softDeleted.ShouldNotBeNull();
            }
        });
    }

    [Fact]
    public async Task Remove_ShouldExcludeRole_FromNormalQueries()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IRolesRepository>();

            var role = Role.Create("Warlord").Value;
            await repository.AddAsync(role);
            await unitOfWork.CommitAsync();

            repository.Remove(role);
            await unitOfWork.CommitAsync();

            var deleted = await repository.GetByIdAsync(role.Id);
            deleted.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Remove_ShouldSetIsDeletedTrue_OnRole()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IRolesRepository>();
            var dbContext = sp.GetRequiredService<TestingRaftelDbContext>();

            var role = Role.Create("Admiral").Value;
            await repository.AddAsync(role);
            await unitOfWork.CommitAsync();

            repository.Remove(role);
            await unitOfWork.CommitAsync();

            var entry = dbContext.Entry(role);
            entry.Property(ShadowPropertyNames.IsDeleted).CurrentValue.ShouldBe(true);
        });
    }

    [Fact]
    public async Task Remove_ShouldStillReturnRole_WhenSoftDeleteFilterIsDisabled()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var repository = sp.GetRequiredService<IRolesRepository>();
            var dataFilter = sp.GetRequiredService<IDataFilter>();

            var role = Role.Create("Yonko").Value;
            await repository.AddAsync(role);
            await unitOfWork.CommitAsync();

            repository.Remove(role);
            await unitOfWork.CommitAsync();

            var deleted = await repository.GetByIdAsync(role.Id);
            deleted.ShouldBeNull();

            using (dataFilter.Disable<ISoftDeleteFilter>())
            {
                var softDeleted = await repository.GetByIdAsync(role.Id);
                softDeleted.ShouldNotBeNull();
            }
        });
    }
}
