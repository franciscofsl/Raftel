namespace Raftel.Infrastructure.Data;

/// <summary>
/// Configuration options for database provider selection.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Gets or sets the database provider to use.
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
}
