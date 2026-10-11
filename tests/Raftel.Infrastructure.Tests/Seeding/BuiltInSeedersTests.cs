using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Application.Authorization;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Authentication;

namespace Raftel.Infrastructure.Tests.Seeding;

public abstract class BuiltInSeedersTestsBase : InfrastructureTestBase
{
    private const string AdminEmail = "admin@example.com";
    private const string StrongPassword = "Str0ng!Passw0rd-for-tests";

    private readonly SeedOptions _options = new() { AdminEmail = AdminEmail, AdminPassword = StrongPassword };
    private readonly TestHostEnvironment _environment = new("Production");
    private readonly CapturingLoggerProvider _logs = new();

    protected BuiltInSeedersTestsBase(IDbContainerFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddRaftelApplication(_ => { });
        services.AddSingleton<IOptions<SeedOptions>>(Options.Create(_options));
        services.AddSingleton<IHostEnvironment>(_environment);
        services.AddLogging(builder => builder.AddProvider(_logs));
    }

    private async Task<Result> RunAsync()
    {
        var result = Result.Success();
        await ExecuteScopedAsync(async sp => result = await sp.GetRequiredService<ISeedRunner>().RunAsync());
        return result;
    }

    private async Task<T> QueryAsync<T>(Func<TestingRaftelDbContext, Task<T>> query)
    {
        T value = default!;
        await ExecuteScopedAsync(async sp => value = await query(sp.GetRequiredService<TestingRaftelDbContext>()));
        return value;
    }

    private IReadOnlyCollection<string> Declared => ServiceProvider.GetRequiredService<IPermissionSetProvider>().PermissionNames;

    // ---- idempotency ----------------------------------------------------------------------

    [Fact]
    public async Task Seeding_RunTwice_ShouldLeaveTheSameState()
    {
        _options.DefaultTenant = new SeedTenantOptions { Name = "Default", Code = "DEFAULT", Description = "d" };

        (await RunAsync()).IsSuccess.ShouldBeTrue();
        var first = await SnapshotAsync();
        (await RunAsync()).IsSuccess.ShouldBeTrue();
        var second = await SnapshotAsync();

        second.ShouldBe(first);
        first.Tenants.ShouldBe(1);
        first.Users.ShouldBe(1);
    }

    private Task<StateCounts> SnapshotAsync() => QueryAsync(async db => new StateCounts(
        await db.Set<Tenant>().CountAsync(),
        await db.Set<PermissionDefinition>().CountAsync(),
        await db.Set<Role>().CountAsync(),
        await db.Set<Permission>().CountAsync(),
        await db.Set<User>().CountAsync(),
        await db.Users.CountAsync(),
        await db.Roles.CountAsync()));

    private sealed record StateCounts(
        int Tenants, int Catalog, int Roles, int RolePermissions, int Users, int IdentityUsers, int IdentityRoles);

    // ---- default tenant -------------------------------------------------------------------

    [Fact]
    public async Task DefaultTenant_ShouldBeCreatedWhenConfigured_AndNotDuplicated()
    {
        _options.DefaultTenant = new SeedTenantOptions { Name = "Default", Code = "DEFAULT", Description = "The default tenant" };

        (await RunAsync()).IsSuccess.ShouldBeTrue();
        (await RunAsync()).IsSuccess.ShouldBeTrue();

        var tenants = await QueryAsync(db => db.Set<Tenant>().ToListAsync());
        tenants.Count.ShouldBe(1);
        tenants[0].Name.ShouldBe("Default");
    }

    [Fact]
    public async Task DefaultTenant_ShouldNotBeCreatedWhenUnconfigured()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();

        (await QueryAsync(db => db.Set<Tenant>().CountAsync())).ShouldBe(0);
    }

    // ---- permissions ----------------------------------------------------------------------

    [Fact]
    public async Task Permissions_ShouldPopulateCatalogOnEmptyDatabase_AndNotDuplicateOnSecondRun()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();
        (await RunAsync()).IsSuccess.ShouldBeTrue();

        var names = await QueryAsync(db => db.Set<PermissionDefinition>().Select(d => d.Name).ToListAsync());
        names.OrderBy(n => n).ShouldBe(Declared.OrderBy(n => n));
    }

    [Fact]
    public async Task Permissions_ShouldAddMissingPermissionWithoutAlteringExistingOnes()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();
        var before = await QueryAsync(db => db.Set<PermissionDefinition>().ToListAsync());
        var removed = before.First();
        await ExecuteScopedAsync(async sp =>
        {
            var db = sp.GetRequiredService<TestingRaftelDbContext>();
            db.Set<PermissionDefinition>().Remove(await db.Set<PermissionDefinition>().SingleAsync(d => d.Id == removed.Id));
            await db.SaveChangesAsync();
        });

        (await RunAsync()).IsSuccess.ShouldBeTrue();

        var after = await QueryAsync(db => db.Set<PermissionDefinition>().ToListAsync());
        after.Count.ShouldBe(before.Count);
        after.Select(d => d.Name).ShouldContain(removed.Name);
        foreach (var untouched in before.Where(d => d.Id != removed.Id))
        {
            after.ShouldContain(d => d.Id == untouched.Id && d.Name == untouched.Name);
        }
    }

    [Fact]
    public async Task Permissions_ShouldFlagUndeclaredPermissionObsoleteWithoutRemovingItFromRoles_AndReinstateWhenItReturns()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();
        await ExecuteScopedAsync(async sp =>
        {
            var db = sp.GetRequiredService<TestingRaftelDbContext>();
            db.Set<PermissionDefinition>().Add(PermissionDefinition.Create("Legacy.Gone").Value);
            var role = Role.Create("Legacy").Value;
            role.AddPermission("Legacy.Gone");
            db.Set<Role>().Add(role);
            await db.SaveChangesAsync();
        });

        (await RunAsync()).IsSuccess.ShouldBeTrue();

        var legacy = await QueryAsync(db => db.Set<PermissionDefinition>().SingleAsync(d => d.Name == "Legacy.Gone"));
        legacy.IsObsolete.ShouldBeTrue();
        _logs.Messages.ShouldContain(m => m.Contains("Legacy.Gone") && m.Contains("obsolete"));
        var role = await QueryAsync(db => db.Set<Role>().SingleAsync(r => r.Name == "Legacy"));
        role.HasPermission("Legacy.Gone").ShouldBeTrue();

        var declared = Declared.First();
        await ExecuteScopedAsync(async sp =>
        {
            var db = sp.GetRequiredService<TestingRaftelDbContext>();
            (await db.Set<PermissionDefinition>().SingleAsync(d => d.Name == declared)).MarkObsolete();
            await db.SaveChangesAsync();
        });

        (await RunAsync()).IsSuccess.ShouldBeTrue();

        (await QueryAsync(db => db.Set<PermissionDefinition>().SingleAsync(d => d.Name == declared))).IsObsolete.ShouldBeFalse();
    }

    // ---- default roles --------------------------------------------------------------------

    [Fact]
    public async Task DefaultRoles_ShouldSeedAdminRoleHoldingEveryActivePermission_AndIdentityRole()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();

        var role = await QueryAsync(db => db.Set<Role>().SingleAsync(r => r.Name == "Admin"));
        role.PermissionNames().OrderBy(n => n).ShouldBe(Declared.OrderBy(n => n));
        await ExecuteScopedAsync(async sp =>
            (await sp.GetRequiredService<RoleManager<IdentityRole>>().FindByNameAsync("Admin")).ShouldNotBeNull());
    }

    [Fact]
    public async Task DefaultRoles_ShouldGrantAMissingPermissionToExistingRole_AndKeepCustomizations()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();
        var missing = Declared.First();
        await ExecuteScopedAsync(async sp =>
        {
            var db = sp.GetRequiredService<TestingRaftelDbContext>();
            var admin = await db.Set<Role>().SingleAsync(r => r.Name == "Admin");
            admin.RemovePermission(missing).IsSuccess.ShouldBeTrue();
            admin.AddPermission("Custom.Manual");
            await db.SaveChangesAsync();
        });

        var rerun = await RunAsync();
        rerun.IsSuccess.ShouldBeTrue(rerun.Error.Message);

        var role = await QueryAsync(db => db.Set<Role>().SingleAsync(r => r.Name == "Admin"));
        role.HasPermission(missing).ShouldBeTrue();
        role.HasPermission("Custom.Manual").ShouldBeTrue();
        (await QueryAsync(db => db.Set<Role>().CountAsync(r => r.Name == "Admin"))).ShouldBe(1);
    }

    [Fact]
    public async Task DefaultRoles_ShouldFailNamingAnUnknownConfiguredPermission()
    {
        _options.Roles.Add(new SeedRoleOptions { Name = "Auditor", Permissions = ["Does.Not.Exist"] });

        var result = await RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Does.Not.Exist");
    }

    // ---- administrator --------------------------------------------------------------------

    [Fact]
    public async Task AdminUser_ShouldBeCreatedWithConfiguredPasswordRoleAndEveryPermission_AndLeftAloneOnSecondRun()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();
        (await RunAsync()).IsSuccess.ShouldBeTrue();

        (await QueryAsync(db => db.Set<User>().CountAsync())).ShouldBe(1);
        await ExecuteScopedAsync(async sp =>
        {
            var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
            var identityUser = await userManager.FindByEmailAsync(AdminEmail);
            identityUser.ShouldNotBeNull();
            (await userManager.CheckPasswordAsync(identityUser, StrongPassword)).ShouldBeTrue();
            (await userManager.IsInRoleAsync(identityUser, "Admin")).ShouldBeTrue();

            var principal = await sp.GetRequiredService<IClaimsPrincipalFactory>().CreateAsync(identityUser);
            principal.FindAll("permission").Select(c => c.Value).OrderBy(v => v).ShouldBe(Declared.OrderBy(n => n));
        });
    }

    [Fact]
    public async Task AdminUser_ShouldFailInProductionWithoutPassword_AndCreateNoUser()
    {
        _options.AdminPassword = null;

        var result = await RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Raftel:Seed:AdminPassword");
        (await QueryAsync(db => db.Set<User>().CountAsync())).ShouldBe(0);
        await ExecuteScopedAsync(async sp =>
            (await sp.GetRequiredService<UserManager<IdentityUser>>().Users.CountAsync()).ShouldBe(0));
    }

    [Fact]
    public async Task AdminUser_ShouldFailInProductionWithoutEmail()
    {
        _options.AdminEmail = null;

        var result = await RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Raftel:Seed:AdminEmail");
        (await QueryAsync(db => db.Set<User>().CountAsync())).ShouldBe(0);
    }

    [Fact]
    public async Task AdminUser_InDevelopmentWithoutPassword_ShouldGenerateOneAndLogItOnlyOnce()
    {
        _environment.EnvironmentName = Environments.Development;
        _options.AdminPassword = null;

        (await RunAsync()).IsSuccess.ShouldBeTrue();
        (await RunAsync()).IsSuccess.ShouldBeTrue();

        (await QueryAsync(db => db.Set<User>().CountAsync())).ShouldBe(1);
        _logs.Messages.Count(m => m.Contains("DEVELOPMENT ONLY")).ShouldBe(1);
    }

    [Fact]
    public async Task AdminUser_ShouldNeverLogAConfiguredPassword()
    {
        (await RunAsync()).IsSuccess.ShouldBeTrue();

        _logs.Messages.ShouldNotContain(m => m.Contains(StrongPassword));
    }

    [Fact]
    public async Task AdminUser_ShouldFailWithPolicyMessageForAWeakPassword_AndCreateNoUser()
    {
        _options.AdminPassword = "a";

        var result = await RunAsync();

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldNotBeNullOrWhiteSpace();
        _logs.Messages.ShouldNotContain(m => m.Contains("'a'"));
        (await QueryAsync(db => db.Set<User>().CountAsync())).ShouldBe(0);
        await ExecuteScopedAsync(async sp =>
            (await sp.GetRequiredService<UserManager<IdentityUser>>().Users.CountAsync()).ShouldBe(0));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Raftel.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _messages = new();
        private readonly object _gate = new();

        public IReadOnlyList<string> Messages
        {
            get
            {
                lock (_gate)
                {
                    return _messages.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        private void Add(string message)
        {
            lock (_gate)
            {
                _messages.Add(message);
            }
        }

        private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                provider.Add(formatter(state, exception));
            }
        }
    }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerBuiltInSeedersTests : BuiltInSeedersTestsBase
{
    public SqlServerBuiltInSeedersTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlBuiltInSeedersTests : BuiltInSeedersTestsBase
{
    public PostgreSqlBuiltInSeedersTests(PostgreSqlTestContainerFixture fixture) : base(fixture)
    {
    }
}
