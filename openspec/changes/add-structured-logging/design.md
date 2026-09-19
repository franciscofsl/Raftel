## Context

See proposal.md - Why. `src/` currently has no `ILogger` usage anywhere and no request correlation mechanism. The command/query pipeline already supports global middleware (`IGlobalMiddleware<TRequest, TResponse>`, registered via `RaftelApplicationBuilder.AddGlobalMiddleware`) in a consumer-defined order, and the ASP.NET layer already has an `ExceptionHandlingMiddleware` that must not leak stack traces to the response body. This design adds logging on top of both without changing either's existing external contracts.

This design follows the "wide events" / canonical-log-line model described in [GitHub issue #115](https://github.com/franciscofsl/Raftel/issues/115): one structured event per request carrying high-cardinality, high-dimensionality context, rather than many scattered log lines. Every AutoEndpoints-mapped route (commands, queries, and the one hand-written `AuthorizationController` action) performs exactly one dispatch, so "one event per HTTP request" and "one event per command/query dispatch" coincide for every request that reaches the pipeline — but not every request does (a malformed JSON body, a 401/403 from ASP.NET's own authorization, or an exception in unrelated middleware never reach a dispatch), so the emission point cannot live inside the command/query pipeline alone.

## Goals / Non-Goals

**Goals:**
- Exactly one structured log entry ("wide event") per HTTP request, regardless of which layer produced its outcome.
- Let request-handling code (handlers, auth context, dependency wrappers) enrich that event with explicitly-named fields.
- A correlation ID that ties every request's wide event back to it, sourced from `X-Correlation-Id` when the client supplies one.
- Guarantee no request payload (potential PII/credentials) ever reaches a log sink by default, and that explicitly enriched fields can't smuggle known-sensitive names in either.

**Non-Goals:**
- Choosing or configuring a concrete logging sink/provider (Serilog, OpenTelemetry, a columnar store, etc.) — left to the consumer, as today for DI.
- Distributed tracing / `Activity`-based correlation (`trace_id`) — tracked separately under the OpenTelemetry backlog item ([15](../../../docs/backlog/15-observabilidad-opentelemetry.md)); issue #115 argues OTel alone doesn't solve this problem, but adding a second identifier is deferred to that change rather than duplicated here.
- Tail sampling / cost-control sampling strategy — every request's wide event is emitted for now; sampling rules (issue #115's "always keep 5xx/exceptions/VIP users, sample the happy path") are a follow-up once there's a concrete sink to sample for.
- A generic `Set`/`Increment` API richer than what's needed to prove the model (e.g. nested object graphs, typed schemas per feature) — start with a flat string-keyed bag; richer shapes are additive later.

## Decisions

### A single emission point, at the ASP.NET boundary
Request-scoped `IRequestEvent` (declared in `Raftel.Application`, since handlers need it) accumulates fields throughout the request. Exactly one component — the outermost ASP.NET middleware, wrapping `ExceptionHandlingMiddleware` — calls `next()`, then emits the event once via `ILogger` after the response outcome (including HTTP status code) is known.
- **Alternative considered**: keep emission inside `Raftel.Application`'s `LoggingMiddleware`, as originally designed. Rejected — `LoggingMiddleware` only runs for requests that reach command/query dispatch. A malformed JSON body, an ASP.NET-level 401/403, or an exception thrown before routing never reach it, so those requests would go completely unlogged under the "one entry per request" goal. The emission point must be the outermost layer that sees every request.

### `LoggingMiddleware` becomes an enricher, not an emitter
`LoggingMiddleware<TRequest, TResponse>` stays first in the global middleware chain and still observes the outcome (success, failed `Result`, or thrown exception), but now writes those into `IRequestEvent` (`request_name`, `level`, `error.code`, `error.message`, or the exception) instead of calling `ILogger` directly. It does **not** set `duration_ms` — that field has exactly one owner, the outermost middleware (see below), since it should measure the whole HTTP request, not just command/query dispatch. It no longer logs a "handling started" line — that line is exactly the "log statements as a debugging diary" anti-pattern issue #115 calls out, and it's incompatible with "exactly one entry per request."
- **Alternative considered**: keep the start-of-request `Information` line for liveness/tracing during long-running requests. Rejected — that's what request-scoped APM/tracing tools are for; a canonical log line's value comes from being singular and complete, not from mid-flight breadcrumbs.

### `ExceptionHandlingMiddleware` becomes an enricher for exception fields
It keeps producing the ProblemDetails response and keeps the guarantee that stack traces never reach the response body, but stops calling `ILogger` itself. Instead it writes the exception and its type-derived `level` (`Debug`/`Warning`/`Error`) into `IRequestEvent` — the required permission for an `UnauthorizedException` is already part of that exception's message, so no separate field is needed for it — for the outer middleware to emit.
- **Alternative considered**: leave `ExceptionHandlingMiddleware` logging independently (the original design). Rejected — that produces a second log line for any request that both goes through `LoggingMiddleware` and throws, violating "exactly one entry."

### `CorrelationIdMiddleware` is extended into the single emission point
Rather than adding a third ASP.NET middleware, `CorrelationIdMiddleware` — already positioned first, already resolving `ICorrelationContext` — is extended to own the request's `IRequestEvent` lifecycle: initialize it, call `next()` inside a `try`/`finally`, set `request_id` (from `ICorrelationContext.CorrelationId`), `method`, `path`, `status_code`, and `duration_ms`, and emit the single log entry at the level `LoggingMiddleware`/`ExceptionHandlingMiddleware` recorded (defaulting to `Information` if nothing set a level, e.g. a 404 from routing with no matching endpoint).
- **Alternative considered**: a brand-new middleware dedicated to wide-event emission. Rejected for now — `CorrelationIdMiddleware` already sits at the right position in the pipeline and already depends on the same request-scoped state; splitting the responsibility into two middlewares that must stay adjacent adds ordering risk without a clear benefit. Revisit if the emission logic outgrows this middleware.
- This replaces the earlier design's use of `logger.BeginScope` to propagate the correlation id — the id is now just another field set directly on `IRequestEvent`, consistent with everything else in the event.

### Field naming convention with an enforced deny-list
Fields use dotted, lower-case names (`user.id`, `pirate.crew`, `db.query_count`), and feature code is expected to expose them as `<Feature>EventFields` constants (mirroring the existing `<Feature>Permissions` convention) rather than inline string literals, so field names are reviewable in a PR diff. Separately — because convention alone doesn't stop a mistake — `IRequestEvent.Set` refuses (silently drops) any field whose name matches a documented deny-list (`password`, `token`, `secret`, `apikey`, `authorization`, case-insensitive substring match).
- **Alternative considered**: rely on code review / the naming convention alone, with no runtime enforcement. Rejected — the whole point of the earlier "never log the payload" guarantee was to not depend on every future contributor remembering the rule; enrichment reopens exactly that risk, so it needs the same default-safe posture.
- **Alternative considered**: throw on a denied field name instead of silently dropping it. Rejected — a logging concern should never be able to fail a production request; same rationale as the correlation sanitizer generating a fresh id instead of rejecting the request.

### Enrichment stays explicit, never automatic reflection
`IRequestEvent` is populated by named `Set`/`Increment` calls; nothing in the framework reflects over a request/command object to auto-populate fields. This reaffirms the original "no request payload in logs" decision, now framed as the necessary default alongside opt-in enrichment rather than the whole story.

## Risks / Trade-offs

- **[Risk]** A shared, ambient field bag (`IRequestEvent`) can become a dumping ground if enrichment isn't disciplined, eroding the low-cardinality/high-signal intent of the model. → **Mitigation**: the `<Feature>EventFields` constant convention plus the enforced deny-list keep additions visible and reviewable; this is a documentation/convention risk, not something the type system alone can prevent.
- **[Risk]** Emitting 100% of requests (no tail sampling yet) means volume scales linearly with traffic and dimensionality — a cost risk once real sinks are wired up at scale. → **Mitigation**: explicitly out of scope for this change (see Non-Goals); tracked as a natural follow-up once a concrete sink exists to sample for.
- **[Risk]** Centralizing emission in `CorrelationIdMiddleware` means a bug there silently loses the *entire* observability signal for a request (previously, `LoggingMiddleware` and `ExceptionHandlingMiddleware` logged independently, so a bug in one didn't erase the other). → **Mitigation**: this is the direct trade-off for guaranteeing "exactly one entry, covering every path including pre-dispatch failures" — the previous design couldn't make that guarantee at all. Cover the emission path itself with tests exercising every outcome (success, business failure, in-pipeline exception, pre-dispatch exception, no matching route).
- **[Risk]** A high-cardinality or malicious correlation ID value still reaches the wide event if it passes the character/length allow-list. → **Mitigation**: unchanged from the original design — sanitization only guarantees no injection/line-forging; the correlation id is a diagnostic hint, not an authenticated identifier.
- **[Risk]** Consumers who forget to register `LoggingMiddleware` first, or who reorder `CorrelationIdMiddleware`/`ExceptionHandlingMiddleware`, silently lose fields or the whole event. → **Mitigation**: documented explicitly in the middleware-order documentation; demo app wiring serves as the reference example.

## Migration Plan

This change already has a first implementation on the `add-structured-logging` branch ([PR #148](https://github.com/franciscofsl/Raftel/pull/148)) built to the *original* design (per-line logging, `ILogger.BeginScope` for correlation). This revision changes the target architecture; the existing code needs rework, not a from-scratch build:
1. Add `IRequestEvent` + its deny-list-enforcing implementation, with unit tests.
2. Rework `LoggingMiddleware` to enrich `IRequestEvent` instead of calling `ILogger`; update its tests accordingly.
3. Rework `ExceptionHandlingMiddleware` to enrich `IRequestEvent` instead of calling `ILogger`; update its tests accordingly.
4. Extend `CorrelationIdMiddleware` to own `IRequestEvent`'s lifecycle and perform the single emission; update/replace the correlation functional tests to also assert "exactly one log entry per request" across every outcome.
5. Update the demo app and layer `CLAUDE.md` docs to describe the new model.

Still purely additive at the public-API level — no consumer-facing type is removed, only `LoggingMiddleware`'s and `ExceptionHandlingMiddleware`'s internal logging behavior changes. No feature flag needed.
