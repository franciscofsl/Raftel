using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Raftel.Application;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Authentication;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;
using Raftel.Infrastructure.Data.Interceptors;
using Raftel.Infrastructure.Data.Repositories.Tenants;
using Raftel.Infrastructure.Data.Repositories.Users;

namespace Raftel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRaftelData<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "Default")
        where TDbContext : RaftelDbContext<TDbContext>
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{connectionStringName}' not found.");

        services.AddDbContext<TDbContext>((serviceProvider, options) => options
            .UseSqlServer(connectionString)
            .UseOpenIddict()
            .AddInterceptors(serviceProvider.GetRequiredService<SoftDeleteInterceptor>()));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TDbContext>());

        services.AddScoped(typeof(IDataFilter), typeof(DataFilter));
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<TDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped(typeof(IUsersRepository), typeof(UsersRepository<TDbContext>));
        services.AddScoped(typeof(ITenantsRepository), typeof(TenantsRepository<TDbContext>));

        services.AddOpenIddict()
            .AddCore(opt => opt.UseEntityFrameworkCore().UseDbContext<TDbContext>())
            .AddServer(opt =>
            {
                opt.SetTokenEndpointUris("/connect/token")
                    .AllowPasswordFlow()
                    .AllowRefreshTokenFlow();

                opt.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                opt.RegisterScopes(OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    "api");

                opt.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();
            })
            .AddValidation(opt =>
            {
                opt.UseLocalServer();
                opt.UseAspNetCore();
            });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentHttpUser>();
        services.AddScoped<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();


        return services;
    }
}