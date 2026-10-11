using Microsoft.EntityFrameworkCore;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Authorization;

namespace Raftel.Infrastructure.Tests.Seeding;

public abstract class PermissionDefinitionPersistenceTestsBase : InfrastructureTestBase
{
    protected PermissionDefinitionPersistenceTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PermissionDefinition_ShouldPersistAndReload()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPermissionDefinitionsRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var definition = PermissionDefinition.Create("Users.View", "View users").Value;
            definition.MarkObsolete();

            await repository.AddAsync(definition);
            await unitOfWork.CommitAsync();

            var loaded = await repository.GetByIdAsync(definition.Id);
            loaded.ShouldNotBeNull();
            loaded.Name.ShouldBe("Users.View");
            loaded.Description.ShouldBe("View users");
            loaded.IsObsolete.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task PermissionDefinition_DuplicateName_ShouldViolateUniqueIndex()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPermissionDefinitionsRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            await repository.AddAsync(PermissionDefinition.Create("Users.View").Value);
            await unitOfWork.CommitAsync();
        });

        await ExecuteScopedAsync(async sp =>
        {
            var repository = sp.GetRequiredService<IPermissionDefinitionsRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            await repository.AddAsync(PermissionDefinition.Create("Users.View").Value);

            await Should.ThrowAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
        });
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerPermissionDefinitionPersistenceTests : PermissionDefinitionPersistenceTestsBase
{
    public SqlServerPermissionDefinitionPersistenceTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlPermissionDefinitionPersistenceTests : PermissionDefinitionPersistenceTestsBase
{
    public PostgreSqlPermissionDefinitionPersistenceTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
