using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Raftel.Application;
using Raftel.Application.Abstractions.Authentication;
using Raftel.Application.Abstractions.Multitenancy;
using Raftel.Domain.Features.Authorization;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.Features.Users;
using Raftel.Infrastructure.Authentication;
using Raftel.Infrastructure.Data;
using Raftel.Infrastructure.Data.Filters;
using Raftel.Infrastructure.Data.Interceptors;
using Raftel.Infrastructure.Data.Repositories.Authorization;
using Raftel.Infrastructure.Data.Repositories.Tenants;
using Raftel.Infrastructure.Data.Repositories.Users;
using Raftel.Infrastructure.Multitenancy;

namespace Raftel.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Raftel data access, authentication, and multitenancy services.
    /// </summary>
    /// <remarks>
    /// In development, ephemeral RSA certificates are generated automatically for OpenIddict token encryption and signing.
    /// In staging and production environments, you must provide real X.509 certificates via
    /// <c>opt.AddEncryptionCertificate(...)</c> and <c>opt.AddSigningCertificate(...)</c>.
    /// </remarks>
    public static IServiceCollection AddRaftelData<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionStringName = "Default")
        where TDbContext : RaftelDbContext<TDbContext>
    {
        services.AddDataAccess<TDbContext>(configuration, connectionStringName);
        services.AddAuthentication<TDbContext>(environment);

        services.AddScoped<ICurrentUser, CurrentHttpUser>();
        services.AddScoped<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
        services.AddScoped<ICurrentTenant, CurrentTenant>();

        return services;
    }

    private static void AddDataAccess<TDbContext>(this IServiceCollection services, IConfiguration configuration,
        string connectionStringName)
        where TDbContext : RaftelDbContext<TDbContext>
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{connectionStringName}' not found.");

        services.Configure<Data.DatabaseOptions>(configuration.GetSection("Database"));

        services.AddDbContext<TDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<Data.DatabaseOptions>>().Value;
            var dbContextOptionsBuilder = databaseOptions.Provider switch
            {
                Data.DatabaseProvider.SqlServer => options.UseSqlServer(connectionString),
                Data.DatabaseProvider.PostgreSql => options.UseNpgsql(connectionString),
                _ => throw new InvalidOperationException($"Unsupported database provider: {databaseOptions.Provider}")
            };

            dbContextOptionsBuilder
                .UseOpenIddict()
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditPropertiesInterceptor>(),
                    serviceProvider.GetRequiredService<TenantInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TDbContext>());

        services.AddScoped(typeof(IDataFilter), typeof(DataFilter));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditPropertiesInterceptor>();
        services.AddScoped<TenantInterceptor>();

        services.AddScoped(typeof(IUsersRepository), typeof(UsersRepository<TDbContext>));
        services.AddScoped(typeof(ITenantsRepository), typeof(TenantsRepository<TDbContext>));
        services.AddScoped(typeof(IRolesRepository), typeof(RolesRepository<TDbContext>));
    }

    private static void AddAuthentication<TDbContext>(this IServiceCollection services, IHostEnvironment environment)
        where TDbContext : RaftelDbContext<TDbContext>
    {
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<TDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddOpenIddict()
            .AddCore(opt => opt.UseEntityFrameworkCore().UseDbContext<TDbContext>())
            .AddServer(opt =>
            {
                opt.SetTokenEndpointUris("/connect/token")
                    .AllowPasswordFlow()
                    .AllowRefreshTokenFlow();

                if (environment.IsDevelopment())
                {
                    // Ephemeral development-only certificates — never use in staging or production.
                    // For staging/production, configure real X.509 certificates:
                    //   opt.AddEncryptionCertificate(certificate)
                    //   opt.AddSigningCertificate(certificate)
                    opt.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }

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

        services.AddHttpContextAccessor();
        services.AddAuthorization();
    }
}