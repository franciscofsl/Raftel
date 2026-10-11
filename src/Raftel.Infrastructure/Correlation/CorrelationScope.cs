using Raftel.Application.Abstractions;

namespace Raftel.Infrastructure.Correlation;

internal sealed class CorrelationScope : ICorrelationScope
{
    private static readonly AsyncLocal<string> Override = new();

    public static string Current => Override.Value;

    public IDisposable Begin(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return NoopScope.Instance;
        }

        var previous = Override.Value;
        Override.Value = correlationId;
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope(string previous) : IDisposable
    {
        public void Dispose() => Override.Value = previous;
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose()
        {
        }
    }
}
