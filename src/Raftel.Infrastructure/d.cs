using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using System;
using System.Threading.Tasks;

namespace MiApiConOpenIddict.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
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

                // Endpoints y flujos
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

                // Scopes estándar
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Scopes.Email);
                // descriptor.Permissions.Add(OpenIddictConstants.Permissions.Scopes.);

                // **¡IMPORTANTE!** permiso para tu API
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "api");

                await mgr.CreateAsync(descriptor);
            }
        }
    }
}