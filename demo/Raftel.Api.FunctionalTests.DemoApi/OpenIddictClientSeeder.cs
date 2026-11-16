using OpenIddict.Abstractions;
using Raftel.Application.Abstractions.Seeding;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.FunctionalTests.DemoApi;

/// <summary>
/// Example of a consumer seeder: registers the OAuth client the demo SPA logs in with. It is
/// discovered automatically because this assembly is registered with <c>AddRaftelApplication</c>,
/// and it is idempotent, so it can run on every startup.
/// </summary>
public sealed class OpenIddictClientSeeder(IOpenIddictApplicationManager applicationManager) : IDataSeeder
{
    private const string ClientId = "web-app";

    public int Order => 10;

    public string Name => "openiddict-web-app-client";

    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await applicationManager.FindByClientIdAsync(ClientId, cancellationToken) is not null)
        {
            return Result.Success();
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = ClientId,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            DisplayName = "SPA Frontend"
        };

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Scopes.Email);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "api");

        await applicationManager.CreateAsync(descriptor, cancellationToken);
        return Result.Success();
    }
}
