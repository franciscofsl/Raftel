using Microsoft.AspNetCore.Routing;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application.Features.Audit;

namespace Raftel.Api.Server.Features.Audit;

public static class AuditDependencyInjection
{
    public static IEndpointRouteBuilder AddRaftelAudit(this IEndpointRouteBuilder app)
    {
        app.AddEndpointGroup(group =>
        {
            group.Name = "Audit";
            group.BaseUri = "/api/audit";
            group.AddQuery<GetEntityAuditHistoryQuery, List<AuditEntryDto>>("{entityName}/{entityId}", HttpMethod.Get);
        });

        return app;
    }
}