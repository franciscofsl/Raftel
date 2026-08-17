## Why

`src/` has zero references to `ILogger`. The framework is completely silent, and the worst symptom is in `ExceptionHandlingMiddleware`: unhandled exceptions are caught and converted into a 500 response without ever being logged, so a production incident leaves no trace of the exception type, stack trace, or which command triggered it. There is also no way to correlate an HTTP request with the events it produces (command executed, domain events dispatched, audit entries written) across log lines.

## What Changes

- Add `Microsoft.Extensions.Logging.Abstractions` to `Raftel.Application` (abstractions only, consistent with the existing `Microsoft.Extensions.*` usage for DI).
- Add a global `LoggingMiddleware<TRequest, TResponse>` as the first middleware in the command/query pipeline: logs start/end of every request with structured message templates, logs a failed `Result` as `Warning` (expected business flow) and a thrown exception as `Error` before rethrowing. Never logs request payload content (only the type name), to avoid leaking credentials or PII.
- Fix `ExceptionHandlingMiddleware` to log the unhandled exception (`Error`, with method/path) before producing the response. The response body still never exposes the stack trace. `ValidationException` logs at `Debug`, `UnauthorizedException` logs at `Warning` with the required permission.
- Introduce `ICorrelationContext` (`Raftel.Application`) and its `Raftel.Infrastructure` implementation backed by `IHttpContextAccessor`: reads `X-Correlation-Id` from the incoming request, generates a new GUID when absent, sanitizes the header value (max 128 chars, alphanumeric/hyphen/underscore only) to prevent log injection/forging, and writes it back on the response.
- Add an ASP.NET `CorrelationIdMiddleware`, positioned before `ExceptionHandlingMiddleware`, that pushes the correlation ID into a logging scope so every log line for the request carries it.
- Document the required middleware order (`LoggingMiddleware` first in the command/query pipeline; `CorrelationIdMiddleware` before `ExceptionHandlingMiddleware` in the ASP.NET pipeline) and wire both into the `demo` app.

## Capabilities

### New Capabilities
- `structured-logging`: request-scoped structured logging for the command/query pipeline (log levels per outcome, no payload/PII leakage, exception logging on the global exception handler).
- `correlation-id`: correlation ID generation/propagation via `X-Correlation-Id`, including input sanitization and logging-scope integration.

### Modified Capabilities
_None — `ExceptionHandlingMiddleware`'s existing behavior (never exposing stack traces in HTTP responses) is preserved; only logging is added to it, which is covered by the new `structured-logging` capability rather than a change to a pre-existing spec._

## Impact

- **New**: `src/Raftel.Application/Middlewares/LoggingMiddleware.cs`, `src/Raftel.Application/Abstractions/ICorrelationContext.cs`, `src/Raftel.Infrastructure/Correlation/CorrelationContext.cs`, `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddleware.cs`, `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddlewareExtensions.cs`, plus unit tests (`LoggingMiddlewareTests`) and functional tests (`CorrelationIdTests`).
- **Modified**: `src/Raftel.Application/Raftel.Application.csproj` and `Directory.Packages.props` (new package reference), `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs`, `src/Raftel.Infrastructure/DependencyInjection.cs`, `demo/**/Program.cs`.
- **Depends on**: typed error taxonomy (already merged, [#146](https://github.com/franciscofsl/Raftel/pull/146)) for `Result`/`Error` shape used in log messages; benefits from cancellation token propagation for consistent request-scoped context.
- Non-breaking, purely additive — no public API removed or renamed.
