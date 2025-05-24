using Raftel.Application.Queries;

namespace Raftel.Application.Features.Tenants.GetCurrentTenant;

public sealed record GetCurrentTenantQuery() : IQuery<GetCurrentTenantResponse>; 