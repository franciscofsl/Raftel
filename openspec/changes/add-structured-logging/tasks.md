> This change was first implemented per-line (see git history / [PR #148](https://github.com/franciscofsl/Raftel/pull/148)), then redesigned to the wide-events model in design.md. Tasks below reflect the current target; §1 and §4's sanitizer/context pieces are already correct and untouched, everything else needs rework.

## 1. Setup

- [x] 1.1 Add `Microsoft.Extensions.Logging.Abstractions` to `Directory.Packages.props` and reference it from `src/Raftel.Application/Raftel.Application.csproj`.

## 2. Wide event core

- [x] 2.1 Define `IRequestEvent` in `src/Raftel.Application/Abstractions/IRequestEvent.cs`: `Set(string field, object value)`, `Increment(string field, long amount = 1)`, `IReadOnlyDictionary<string, object> Fields { get; }`.
- [x] 2.2 Implement a request-scoped `RequestEvent` (landed in `Raftel.Application/Abstractions/RequestEvent.cs` — no ASP.NET dependency needed) enforcing the sensitive-name deny-list (`password`, `token`, `secret`, `apikey`, `authorization`, case-insensitive substring match) inside `Set`: a denied field name is silently dropped, never throws. Also added `RequestEventFields` (well-known field-name constants shared by framework middleware).
- [x] 2.3 (test) Deny-list unit tests: each denied name (and a case-varied form of it) is dropped; an allowed name is stored; `Increment` accumulates correctly across multiple calls.
- [x] 2.4 Register `IRequestEvent`/`RequestEvent` as Scoped in DI.

## 3. Single emission point

- [x] 3.1 Rework `src/Raftel.Application/Middlewares/LoggingMiddleware.cs`: stop calling `ILogger` directly; set `request_name` and outcome fields (`level` + `error.code`/`error.message` on failure, `level` + the exception on throw) on `IRequestEvent` instead. Remove the start-of-request log call entirely. (`duration_ms` is owned solely by the outer emission point, §3.5 — see the design.md fix for why it isn't set here too.)
- [x] 3.2 (test) Rewrite `tests/Raftel.Application.UnitTests/Middlewares/LoggingMiddlewareTests.cs` to assert against `IRequestEvent` state directly (dropped the now-unused `RecordingLogger` test double), not `ILogger` calls.
- [x] 3.3 Rework `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs`: stop calling `ILogger` directly; enrich `IRequestEvent` with the exception and its type-derived severity (`Debug`/`Warning`/`Error` via `RequestEventFields.Level`) instead. Kept producing the ProblemDetails response and the no-stack-trace-in-body guarantee unchanged. (No separate "required permission" field — it's already in `UnauthorizedException.Message`, carried via the `exception` field.)
- [x] 3.4 (test) Rewrite `tests/Raftel.Api.FunctionalTests/ExceptionHandlingTests.cs`'s log-level assertions to read from the emitted wide event (via `TestLogCapture`, filtered to the `CorrelationIdMiddleware` category) rather than expecting a log call from `ExceptionHandlingMiddleware` itself, and assert exactly one entry per request.
- [x] 3.5 Extend `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddleware.cs` to own `IRequestEvent`'s lifecycle: initialize it, call `next()` in a `try`/`finally`, set `request_id` (from `ICorrelationContext.CorrelationId`), `method`, `path`, `status_code`, `duration_ms`, and emit exactly one `ILogger` entry at the severity level recorded by §3.1/§3.3 (default `Information` when nothing set one, e.g. a 404 with no matching route). Extended `TestLogCapture`/`TestLoggerProvider`/`CapturedLogEntry` (functional test support) to also capture the raw `state` object so tests can assert on emitted fields, not just the formatted message.
- [x] 3.6 (test funcional) `tests/Raftel.Api.FunctionalTests/WideEventTests.cs`: exactly one log entry per request for every outcome — handler success (`Information`), business `Result` failure (`Warning`), in-pipeline exception, malformed JSON body (pre-dispatch, no exception actually thrown — handled inline by the endpoint mapper), and a request with no matching route.

## 4. Correlation ID feeds the wide event

- [x] 4.1 (test) Unit tests for the correlation header sanitizer — unchanged, still valid.
- [x] 4.2 Define `ICorrelationContext` in `src/Raftel.Application/Abstractions/ICorrelationContext.cs` — unchanged, still valid.
- [x] 4.3 Implement `src/Raftel.Infrastructure/Correlation/CorrelationContext.cs` — unchanged, still valid.
- [x] 4.4 Register `ICorrelationContext`/`CorrelationContext` in DI — unchanged, still valid.
- [x] 4.5 (test funcional) Update `tests/Raftel.Api.FunctionalTests/CorrelationIdTests.cs`: keep the existing header round-trip/sanitization scenarios, and add one asserting the request's emitted wide event includes `request_id` matching the response header's value.
- [x] 4.6 Superseded by §3.5 — folded into it, no separate work.
- [x] 4.7 Wire `CorrelationIdMiddleware` into the ASP.NET pipeline before `ExceptionHandlingMiddleware` — unchanged, still valid (still needs to stay outermost).

## 5. Demo and documentation

- [x] 5.1 Wire `LoggingMiddleware` and `CorrelationIdMiddleware` into `demo/**/Program.cs` — unchanged, still valid.
- [x] 5.2 Update `src/Raftel.Application/CLAUDE.md` and `src/Raftel.Api.Server/CLAUDE.md`: replace the "LoggingMiddleware logs start/outcome" description with the wide-event model (`IRequestEvent`, enrichment, the deny-list, single emission from `CorrelationIdMiddleware`).

## 6. Verification

- [x] 6.1 Run `dotnet test` for the full suite and confirm all new and reworked tests pass.
- [x] 6.2 Manually verify against the (updated) backlog acceptance criteria: no exception is swallowed without being logged; no stack trace appears in any HTTP response; no log entry contains passwords or tokens (default or denied-field-name enrichment attempt); every request produces exactly one log entry; that entry carries the request's `CorrelationId`/`request_id`; `X-Correlation-Id` is present on every response.
