using System.Collections.Concurrent;

namespace Raftel.Api.FunctionalTests.TestSupport;

public sealed class TestLogCapture
{
    private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

    public IReadOnlyList<CapturedLogEntry> Entries => _entries.ToList();

    public void Add(CapturedLogEntry entry) => _entries.Enqueue(entry);

    public void Clear() => _entries.Clear();
}
