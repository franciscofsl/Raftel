using Microsoft.Extensions.Logging;

namespace Raftel.Api.FunctionalTests.TestSupport;

public sealed record CapturedLogEntry(string Category, LogLevel Level, string Message, Exception Exception, object State);
