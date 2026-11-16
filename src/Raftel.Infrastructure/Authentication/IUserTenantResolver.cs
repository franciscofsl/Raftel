namespace Raftel.Infrastructure.Authentication;

internal interface IUserTenantResolver
{
    Task<Guid?> ResolveAsync(string identityUserId, CancellationToken cancellationToken = default);
}
