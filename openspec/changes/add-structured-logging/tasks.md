## 1. Setup

- [ ] 1.1 Add `Microsoft.Extensions.Logging.Abstractions` to `Directory.Packages.props` and reference it from `src/Raftel.Application/Raftel.Application.csproj`.

## 2. Structured logging — `LoggingMiddleware`

- [ ] 2.1 (test) Write `tests/Raftel.Application.UnitTests/Middlewares/LoggingMiddlewareTests.cs` using a substituted `ILogger` (NSubstitute): successful request logs `Information` on start and completion; failed `Result` logs `Warning` with the error code and no `Error`; thrown exception logs `Error` with the exception and rethrows.
- [ ] 2.2 (test) Extend the same test file to assert the middleware never logs any property value of the request — only `typeof(TRequest).Name` and metadata (elapsed time, error code).
- [ ] 2.3 Implement `src/Raftel.Application/Middlewares/LoggingMiddleware.cs` per design.md, satisfying the tests in 2.1/2.2.
- [ ] 2.4 Register `LoggingMiddleware<,>` as the first global middleware in the framework's DI wiring and document (in the framework's middleware-order documentation) that it must remain first.

## 3. Structured logging — `ExceptionHandlingMiddleware` fix

- [ ] 3.1 (test) Add/extend tests for `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs` covering: unhandled exception logs `Error` with method/path and the response body still contains no stack trace; `ValidationException` logs `Debug`; `UnauthorizedException` logs `Warning` including the required permission.
- [ ] 3.2 Update `ExceptionHandlingMiddleware` to log before producing each response, per design.md, satisfying the tests in 3.1.

## 4. Correlation ID

- [ ] 4.1 (test) Write unit tests for the correlation header sanitizer: valid header (alphanumeric/hyphen/underscore, ≤128 chars) is reused; header >128 chars is discarded and a new ID generated; header with disallowed characters (including line breaks/control characters) is discarded and a new ID generated.
- [ ] 4.2 Define `ICorrelationContext` in `src/Raftel.Application/Abstractions/ICorrelationContext.cs`.
- [ ] 4.3 Implement `src/Raftel.Infrastructure/Correlation/CorrelationContext.cs` against `IHttpContextAccessor`, applying the sanitizer from 4.1 and generating a new GUID when the header is absent or invalid.
- [ ] 4.4 Register `ICorrelationContext`/`CorrelationContext` in `src/Raftel.Infrastructure/DependencyInjection.cs`.
- [ ] 4.5 (test funcional) Write `tests/Raftel.Api.FunctionalTests/CorrelationIdTests.cs`: a request with a known `X-Correlation-Id` gets the same value back on the response; a request without the header gets a generated value back; a malicious header (200 chars, embedded line breaks) is sanitized and a new value is returned instead.
- [ ] 4.6 Implement `src/Raftel.Api.Server/Middlewares/CorrelationIdMiddleware.cs` and `CorrelationIdMiddlewareExtensions.cs`: resolve the correlation ID via `ICorrelationContext`, push it into the ASP.NET logging scope, and set it on the response header, satisfying the tests in 4.5.
- [ ] 4.7 Wire `CorrelationIdMiddleware` into the ASP.NET pipeline before `ExceptionHandlingMiddleware`.

## 5. Demo and documentation

- [ ] 5.1 Wire `LoggingMiddleware` and `CorrelationIdMiddleware` into `demo/**/Program.cs`.
- [ ] 5.2 Document the required middleware order (`LoggingMiddleware` first among global command/query middleware; `CorrelationIdMiddleware` before `ExceptionHandlingMiddleware` in the ASP.NET pipeline) in the relevant `CLAUDE.md`/README for the affected layers.

## 6. Verification

- [ ] 6.1 Run `dotnet test` for the full suite and confirm all new and existing tests pass.
- [ ] 6.2 Manually verify against the acceptance criteria in the backlog item: no exception is swallowed without being logged; no stack trace appears in any HTTP response; no log contains passwords or tokens; all events for one request share a `CorrelationId`; `X-Correlation-Id` is present on every response.
