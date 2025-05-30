using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Authorization.ValueObjects;

namespace Raftel.Domain.Features.Authorization;

public interface IRolesRepository : IRepository<Role, RoleId>
{
} 