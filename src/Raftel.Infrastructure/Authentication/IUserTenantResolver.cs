namespace Raftel.Infrastructure.Authentication;

/// <summary>
/// Resolves the tenant an identity user belongs to, from the domain user bound to it, independently of
/// the tenant of the current request.
/// </summary>
internal interface IUserTenantResolver
{
    /// <summary>
    /// Returns the tenant of the domain user bound to <paramref name="identityUserId"/>, or <c>null</c> when
    /// the user is a host user (or no domain user is bound to it).
    /// </summary>
    Task<Guid?> ResolveAsync(string identityUserId, CancellationToken cancellationToken = default);
}
