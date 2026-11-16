using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions.Seeding;

/// <summary>
/// Seeds initial data at startup. Implementations must be idempotent: running them any number of
/// times against unchanged code and configuration must leave the database in the same state.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Gets the execution order. Lower values run first; ties are broken by <see cref="Name"/>.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets the unique name used to identify this seeder in logs and in the seed history.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of this seeder. Raising it makes a <see cref="RunOnce"/> seeder execute again.
    /// </summary>
    int Version => 1;

    /// <summary>
    /// Gets a value indicating whether the seeder is skipped once applied at its current version.
    /// Reconciling seeders (the default) execute on every run.
    /// </summary>
    bool RunOnce => false;

    /// <summary>
    /// Seeds the data. Business failures are reported through the returned <see cref="Result"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<Result> SeedAsync(CancellationToken cancellationToken = default);
}
