using Raftel.Application.Queries;

namespace Raftel.Application.Features.Tenants.GetAllTenants;

public sealed record GetAllTenantsQuery() : IQuery<List<GetAllTenantsResponse>>; 