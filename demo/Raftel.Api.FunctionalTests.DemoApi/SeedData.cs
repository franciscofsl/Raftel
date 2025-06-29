using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Raftel.Application.Features.Tenants;
using Raftel.Application.Features.Users;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Features.Authorization;

namespace Raftel.Api.FunctionalTests.DemoApi;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<TestingRaftelDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
            
        await SetupOpenIddict(services);

        var adminRole = await dbContext.Role.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is not null)
        {
            adminRole.AddPermissions([PiratesPermissions.Management, PiratesPermissions.View]);
            adminRole.AddPermissions([TenantsPermissions.Management, TenantsPermissions.View]);
            return;
        }
        
        adminRole = Role.Create("admin", "Administrator role with full access to the system").Value;
        
        adminRole.AddPermissions([PiratesPermissions.Management, PiratesPermissions.View]);
        adminRole.AddPermissions([TenantsPermissions.Management, TenantsPermissions.View]);
        adminRole.AddPermissions([UsersPermissions.View]);

        await dbContext.Role.AddAsync(adminRole);
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