using Microsoft.AspNetCore.Routing;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application.Features.Tenants.CreateTenant;
using Raftel.Application.Features.Tenants.GetTenant;
using Raftel.Application.Features.Tenants.GetAllTenants;
using Raftel.Application.Features.Tenants.GetCurrentTenant;
using Raftel.Domain.Abstractions;

namespace Raftel.Api.Server.Features.Tenants;

public static class TenantsDependencyInjection
{
    public static IEndpointRouteBuilder AddRaftelTenants(this IEndpointRouteBuilder app)
    {
        app.AddEndpointGroup(group =>
        {
            group.Name = "Tenants";
            group.BaseUri = "/api/tenants";
            group.AddCommand<CreateTenantCommand>("", HttpMethod.Post);
            group.AddQuery<GetTenantQuery, GetTenantResponse>("{id}", HttpMethod.Get);
            group.AddQuery<GetAllTenantsQuery, PagedResult<GetAllTenantsResponse>>("", HttpMethod.Get);
            group.AddQuery<GetCurrentTenantQuery, GetCurrentTenantResponse>("current", HttpMethod.Get);
        });

        return app;
    }
} 