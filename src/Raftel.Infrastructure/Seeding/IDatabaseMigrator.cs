namespace Raftel.Infrastructure.Seeding;

/// <summary>
/// Applies pending database migrations.
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>
    /// Applies every pending migration.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task MigrateAsync(CancellationToken cancellationToken);
}
