namespace Raftel.Application.Abstractions.Multitenancy;

/// <summary>
/// Interface that represents the current tenant in a multitenancy system.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// Gets the identifier of the current tenant, if available.
    /// </summary>
    public Guid? Id { get; }

    /// <summary>
    /// Changes the context to the specified tenant.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant to switch the context to. Can be null.</param>
    /// <returns>An <see cref="IDisposable"/> object that, when disposed, reverts the context change.</returns>
    IDisposable Change(Guid? tenantId);
}