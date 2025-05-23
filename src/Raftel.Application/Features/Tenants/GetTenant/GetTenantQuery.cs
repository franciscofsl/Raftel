using Raftel.Application.Queries;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Features.Tenants.GetTenant;

public sealed record GetTenantQuery(Guid Id) : IQuery<GetTenantResponse>; 