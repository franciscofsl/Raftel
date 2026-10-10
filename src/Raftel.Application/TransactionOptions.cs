using System.Data;

namespace Raftel.Application;

/// <summary>
/// Options controlling the transaction opened around command handling.
/// </summary>
public sealed class TransactionOptions
{
    /// <summary>
    /// Gets or sets the isolation level used when starting a command transaction.
    /// Defaults to <see cref="IsolationLevel.ReadCommitted"/>.
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}
