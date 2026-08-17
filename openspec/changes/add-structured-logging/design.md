## Context

See proposal.md - Why. `src/` currently has no `ILogger` usage anywhere and no request correlation mechanism. The command/query pipeline already supports global middleware (`IGlobalMiddleware<TRequest, TResponse>`, registered via `RaftelApplicationBuilder.AddGlobalMiddleware`) in a consumer-defined order, and the ASP.NET layer already has an `ExceptionHandlingMiddleware` that must not leak stack traces to the response body. This design adds logging on top of both without changing either's existing external contracts.

## Goals / Non-Goals

**Goals:**
- Structured, provider-agnostic logging (`ILogger` abstractions only) for the command/query pipeline and for previously-swallowed exceptions.
- A correlation ID that ties together every log line for a single HTTP request, sourced from `X-Correlation-Id` when the client supplies one.
- Guarantee no request payload (potential PII/credentials) ever reaches a log sink through the new middleware.

**Non-Goals:**
- Choosing or configuring a concrete logging sink/provider (Serilog, OpenTelemetry, etc.) — left to the consumer, as today for DI.
- Distributed tracing / `Activity`-based correlation — tracked separately under the OpenTelemetry backlog item ([15](../../../docs/backlog/15-observabilidad-opentelemetry.md) if/when proposed).
- Opt-in payload logging (`[LogPayload]` attribute) — future extension, not required for this change.

## Decisions

### `LoggingMiddleware<TRequest, TResponse>` as first global middleware
Implemented in `Raftel.Application` as an `IGlobalMiddleware<TRequest, TResponse>`, registered first via `AddGlobalMiddleware` so it wraps every other middleware (validation, permission checks, etc.) and captures their elapsed time and failures too.
- **Alternative considered**: hooking logging into the mediator's dispatch loop directly. Rejected — the existing middleware pipeline is the established extension point (Object Calisthenics / composition over ad hoc hooks), and keeps logging swappable/removable like any other middleware.

### Log request type name only, never payload
`LoggingMiddleware` logs `typeof(TRequest).Name` and structured metadata (elapsed time, error code) but never serializes or reflects into the request object's properties.
- **Alternative considered**: logging the full request as structured data for richer diagnostics. Rejected — commands routinely carry passwords/tokens (e.g. `RegisterUserCommand`); default-safe beats default-rich. An opt-in `[LogPayload]` attribute is deferred to a future change if needed.

### `Result` failure → `Warning`; exception → `Error`
A failed `Result` is expected business flow (validation failure, not-found, conflict) and does not indicate a defect, so it is logged at `Warning`. An exception is unexpected and always logged at `Error` with the exception object attached, then rethrown so `ExceptionHandlingMiddleware` still owns producing the HTTP response.
- **Alternative considered**: logging both at the same level. Rejected — collapsing them would drown real defects in expected business-rule rejections (404s, validation errors) in monitoring/alerting.

### Correlation ID as an explicit header, resolved in `Raftel.Infrastructure`
`ICorrelationContext` is declared in `Raftel.Application` (so command/query middleware can reference it) but implemented in `Raftel.Infrastructure` against `IHttpContextAccessor`, following the existing dependency direction (Application defines abstractions, Infrastructure implements them against ASP.NET/EF Core concerns).
- **Alternative considered**: using `Activity.Current.Id` (W3C trace context) instead of a custom header. Rejected for *this* change — it requires tracing to be configured to have any value, whereas an explicit `X-Correlation-Id` works standalone and is simpler for consumers without OpenTelemetry set up. Revisit when/if the OpenTelemetry backlog item lands; the two are not mutually exclusive.

### Correlation header sanitization: allow-list validation
Incoming `X-Correlation-Id` is validated against `^[A-Za-z0-9_-]{1,128}$`; anything else is discarded and a new GUID is generated instead of rejecting the request. Sanitization happens once, at the point the header is read, before the value ever reaches a logging scope.
- **Alternative considered**: rejecting the request (400) on an invalid header. Rejected — correlation is a diagnostic aid, not a contract; refusing to serve a valid business request over a malformed diagnostic header is worse than silently issuing a fresh ID.

### Two separate capabilities, one change
`structured-logging` and `correlation-id` are modeled as separate spec capabilities because they have independent value (logging works without correlation; correlation could feed future tracing work), but are proposed and implemented together since correlation IDs are only useful once logging exists to carry them, and `LoggingMiddleware` scopes include the correlation ID from day one.

## Risks / Trade-offs

- **[Risk]** A high-cardinality or malicious correlation ID value still reaches logs if it passes the character/length allow-list (e.g. a plausible-looking but attacker-chosen ID used to correlate/poison unrelated log analysis). → **Mitigation**: sanitization only guarantees no injection/line-forging; treating correlation ID as a diagnostic hint rather than an authenticated identifier is an accepted trade-off, consistent with its purpose.
- **[Risk]** `LoggingMiddleware` wrapping every request adds per-request overhead (`Stopwatch`, `BeginScope`, two log calls). → **Mitigation**: overhead is the same order of magnitude as any structured logging middleware; `ILogger` filtering means disabled log levels short-circuit cheaply, and this is already accepted practice per the referenced logging best-practices guide.
- **[Risk]** Consumers who forget to register `LoggingMiddleware` first (per docs) lose visibility into earlier middleware's failures. → **Mitigation**: documented explicitly in the registration section (proposal `2.5`) and demo app wiring serves as the reference example.

## Migration Plan

Purely additive — no existing public API is removed or changed. Rollout is: add package reference → implement and test `LoggingMiddleware` → fix `ExceptionHandlingMiddleware` logging → implement and test `CorrelationIdMiddleware`/`CorrelationContext` → wire both into `demo`. Each step ships independently and can be merged incrementally; no feature flag needed since nothing is removed and default behavior (respond correctly) is unchanged, only observability is added. No rollback concerns beyond a normal revert.
