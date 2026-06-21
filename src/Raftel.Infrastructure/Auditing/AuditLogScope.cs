using Raftel.Application.Abstractions.Auditing;

namespace Raftel.Infrastructure.Auditing;

/// <summary>
/// <see cref="AsyncLocal{T}"/>-based implementation of <see cref="IAuditLogScope"/>, following
/// the same ambient-context pattern used by <c>CurrentTenant</c>.
/// </summary>
internal sealed class AuditLogScope : IAuditLogScope
{
    private static readonly AsyncLocal<string?> CurrentCommand = new();

    public string? Command => CurrentCommand.Value;

    public IDisposable Begin(string command)
    {
        return new Scope(command);
    }

    private sealed class Scope : IDisposable
    {
        private readonly string? _previousCommand;

        public Scope(string command)
        {
            _previousCommand = CurrentCommand.Value;
            CurrentCommand.Value = command;
        }

        public void Dispose()
        {
            CurrentCommand.Value = _previousCommand;
        }
    }
}
