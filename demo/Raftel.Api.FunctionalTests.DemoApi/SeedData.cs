using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Features.Tenants;
using Raftel.Application.Features.Users;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Users;

namespace Raftel.Api.FunctionalTests.DemoApi;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<TestingRaftelDbContext>();
        await dbContext.Database.MigrateAsync();
            
        await SetupOpenIddict(services);

        var adminRole = await dbContext.Role.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is null)
        {
            adminRole = Role.Create("admin", "Administrator role with full access to the system").Value;
            await dbContext.Role.AddAsync(adminRole);
        }

        adminRole.AddPermissions([PiratesPermissions.Management, PiratesPermissions.View]);
        adminRole.AddPermissions([TenantsPermissions.Management, TenantsPermissions.View, TenantsPermissions.Switch]);
        adminRole.AddPermissions([UsersPermissions.View, UsersPermissions.AssignRoleToUser]);
        await dbContext.SaveChangesAsync();

        await SeedAdministratorAsync(services, dbContext, adminRole);
    }

    private static async Task SeedAdministratorAsync(IServiceProvider services, TestingRaftelDbContext dbContext, Role adminRole)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration["Raftel:Seed:AdminEmail"];
        var password = configuration["Raftel:Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(adminRole.Name))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole.Name));
        }

        var authentication = services.GetRequiredService<IAuthenticationService>();
        var existing = await dbContext.User.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
        {
            await authentication.AssignRoleAsync(existing, adminRole);
            return;
        }

        var user = User.Create(email, email, email);
        var registered = await authentication.RegisterAsync(user, password);
        if (registered.IsFailure)
        {
            throw new InvalidOperationException($"Administrator could not be created: {registered.Error.Message}");
        }

        user.BindTo(registered.Value);
        await dbContext.User.AddAsync(user);
        await authentication.AssignRoleAsync(user, adminRole);
        user.AssignRole(adminRole);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SetupOpenIddict(IServiceProvider services)
    {
        var mgr = services.GetRequiredService<IOpenIddictApplicationManager>();

        if (await mgr.FindByClientIdAsync("web-app") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId    = "web-app",
                ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                DisplayName = "SPA Frontend"
            };

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Scopes.Email);

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "api");

            await mgr.CreateAsync(descriptor);
        }
    }
}