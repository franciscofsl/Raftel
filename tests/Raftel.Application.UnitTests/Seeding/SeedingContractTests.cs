using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Application.Authorization;
using Raftel.Application.Features.Tenants;
using Raftel.Application.Features.Users;
using Shouldly;

namespace Raftel.Application.UnitTests.Seeding;

public class SeedingContractTests
{
    [Fact]
    public void MinimalSeeder_ShouldDefaultToVersionOneAndReconcile()
    {
        IDataSeeder seeder = new MinimalSeeder();

        seeder.Version.ShouldBe(1);
        seeder.RunOnce.ShouldBeFalse();
    }

    [Fact]
    public void SeedOptions_ShouldHaveSafeDefaults()
    {
        var options = new SeedOptions();

        options.ApplyMigrationsOnStartup.ShouldBeFalse();
        options.RunSeedersOnStartup.ShouldBeTrue();
        options.FailFastOnError.ShouldBeTrue();
        options.AdminPassword.ShouldBeNull();
        options.AdminEmail.ShouldBeNull();
        options.DefaultTenant.ShouldBeNull();
        options.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void AddRaftelApplication_ShouldRegisterSeeders_FromScannedAssembly()
    {
        var provider = BuildServiceProvider();

        var seeders = provider.GetServices<IDataSeeder>().ToList();

        seeders.ShouldContain(seeder => seeder is MinimalSeeder);
    }

    [Fact]
    public void AddRaftelApplication_ShouldNotRegisterAbstractSeeders()
    {
        var provider = BuildServiceProvider();

        var seeders = provider.GetServices<IDataSeeder>().ToList();

        seeders.ShouldNotContain(seeder => seeder.GetType() == typeof(AbstractSeeder));
    }

    [Fact]
    public void PermissionSetProvider_ShouldReturnBuiltInPermissionConstants()
    {
        var names = BuildServiceProvider().GetRequiredService<IPermissionSetProvider>().PermissionNames;

        names.ShouldContain(UsersPermissions.View);
        names.ShouldContain(UsersPermissions.AssignRoleToUser);
        names.ShouldContain(UsersPermissions.Create);
        names.ShouldContain(UsersPermissions.Edit);
        names.ShouldContain(UsersPermissions.Delete);
        names.ShouldContain(TenantsPermissions.Management);
        names.ShouldContain(TenantsPermissions.View);
    }

    [Fact]
    public void PermissionSetProvider_ShouldReturnConsumerMarkedConstantsOnly()
    {
        var names = BuildServiceProvider().GetRequiredService<IPermissionSetProvider>().PermissionNames;

        names.ShouldContain(SamplePermissions.Read);
        names.ShouldContain(SamplePermissions.Write);
        names.ShouldNotContain(SamplePermissions.NotAConstant);
    }

    [Fact]
    public void PermissionSetProvider_ShouldIgnoreUnmarkedClasses()
    {
        var names = BuildServiceProvider().GetRequiredService<IPermissionSetProvider>().PermissionNames;

        names.ShouldNotContain(UnmarkedPermissions.Hidden);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddRaftelApplication(cfg => cfg.RegisterServicesFromAssembly(typeof(MinimalSeeder).Assembly));
        return services.BuildServiceProvider();
    }
}
