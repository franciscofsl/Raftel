## Why

`src/` has zero references to `ILogger`. The framework is completely silent, and the worst symptom is in `ExceptionHandlingMiddleware`: unhandled exceptions are caught and converted into a 500 response without ever being logged, so a production incident leaves no trace of the exception type, stack trace, or which command triggered it. There is also no way to correlate an HTTP request with the events it produces (command executed, domain events dispatched, audit entries written) across log lines.

Per [GitHub issue #115](https://github.com/franciscofsl/Raftel/issues/115), scattered per-line logging is itself the problem this change should avoid reintroducing: a typical request producing 10-20 disconnected log lines is what makes debugging under pressure hard in the first place. This proposal adopts the "wide events" / canonical-log-line model instead — one structured, high-dimensionality event per request.

## What Changes

- Add `Microsoft.Extensions.Logging.Abstractions` to `Raftel.Application` (abstractions only, consistent with the existing `Microsoft.Extensions.*` usage for DI).
- Add a request-scoped `IRequestEvent` (`Raftel.Application`): a field bag that request-handling code (handlers, auth context, dependency wrappers) enriches with named fields throughout the request's lifecycle, with a deny-list refusing sensitive field names (`password`, `token`, `secret`, `apikey`, `authorization`) even when explicitly set.
- Add a global `LoggingMiddleware<TRequest, TResponse>` as the first middleware in the command/query pipeline: enriches `IRequestEvent` with the request name, elapsed time, and outcome (failed `Result` → warning-level fields with the error code/message; thrown exception → error-level fields with the exception) — it does not log directly and never includes request payload content, to avoid leaking credentials or PII.
- Fix `ExceptionHandlingMiddleware` to enrich `IRequestEvent` with the caught exception and its type-derived severity (`Debug` for `ValidationException`, `Warning` with the required permission for `UnauthorizedException`, `Error` otherwise) before producing the response. The response body still never exposes the stack trace.
- Introduce `ICorrelationContext` (`Raftel.Application`) and its `Raftel.Infrastructure` implementation backed by `IHttpContextAccessor`: reads `X-Correlation-Id` from the incoming request, generates a new GUID when absent, sanitizes the header value (max 128 chars, alphanumeric/hyphen/underscore only) to prevent log injection/forging, and writes it back on the response.
- Extend `CorrelationIdMiddleware`, positioned outermost (wrapping `ExceptionHandlingMiddleware`), to own the request's `IRequestEvent` lifecycle: initialize it, set `request_id`/`method`/`path`/`status_code`/`duration_ms`, and emit **exactly one** structured log entry per HTTP request — covering requests that never reach command/query dispatch (malformed JSON, ASP.NET-level 401/403, routing 404), not only ones that do.
- Document the required middleware order (`LoggingMiddleware` first in the command/query pipeline; `CorrelationIdMiddleware` outermost, before `ExceptionHandlingMiddleware`, in the ASP.NET pipeline) and wire both into the `demo` app.

## Capabilities

### New Capabilities
- `structured-logging`: exactly one structured "wide event" per HTTP request, explicit field enrichment with an enforced sensitive-name deny-list, no payload/PII leakage by default, exception severity mapped by type.
- `correlation-id`: correlation ID generation/propagation via `X-Correlation-Id`, including input sanitization and inclusion in the request's wide event.

### Modified Capabilities
_None — `ExceptionHandlingMiddleware`'s existing behavior (never exposing stack traces in HTTP responses) is preserved; only logging is added to it, which is covered by the new `structured-logging` capability rather than a change to a pre-existing spec._

## Impact

- **New**: `src/Raftel.Application/Abstractions/IRequestEvent.cs` (+ implementation, likely in `Raftel.Infrastructure`), `src/Raftel.Application/Middlewares/LoggingMiddleware.cs`, `src/Raftel.Application/Abstractions/ICorrelationContext.cs`, `src/Raftel.Infrastructure/Correlation/CorrelationContext.cs`, `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddleware.cs`, `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddlewareExtensions.cs`, plus unit tests (`LoggingMiddlewareTests`, `IRequestEvent` deny-list tests) and functional tests (`CorrelationIdTests`, single-entry-per-request tests).
- **Modified**: `src/Raftel.Application/Raftel.Application.csproj` and `Directory.Packages.props` (new package reference), `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs`, `src/Raftel.Infrastructure/DependencyInjection.cs`, `demo/**/Program.cs`.
- **Depends on**: typed error taxonomy (already merged, [#146](https://github.com/franciscofsl/Raftel/pull/146)) for `Result`/`Error` shape used in log fields; benefits from cancellation token propagation for consistent request-scoped context.
- **Supersedes**: the per-line-logging implementation already on this branch ([PR #148](https://github.com/franciscofsl/Raftel/pull/148)) — see design.md's Migration Plan for the rework path.
- Non-breaking, purely additive at the public-API level — no public type removed or renamed; only the internal logging behavior of `LoggingMiddleware`/`ExceptionHandlingMiddleware` changes.
