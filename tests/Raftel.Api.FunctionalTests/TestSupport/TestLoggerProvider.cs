using Microsoft.Extensions.Logging;

namespace Raftel.Api.FunctionalTests.TestSupport;

public sealed class TestLoggerProvider(TestLogCapture capture) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, capture);

    public void Dispose()
    {
    }

    private sealed class TestLogger(string category, TestLogCapture capture) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            capture.Add(new CapturedLogEntry(category, logLevel, formatter(state, exception), exception, state));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
