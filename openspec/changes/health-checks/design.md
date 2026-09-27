## Context

See proposal.md — Why. Constraints that shape the approach:

- **Layering is non-negotiable** (`CLAUDE.md`): `Raftel.Api.Server` depends only on `Raftel.Infrastructure`, and `Microsoft.EntityFrameworkCore` is forbidden in `Domain` and in the API layer. Every check that touches EF Core must therefore live in `Raftel.Infrastructure`; `Raftel.Api.Server` may only map endpoints and serialize results.
- **The DbContext is generic**: `RaftelDbContext<TDbContext>`, registered through `AddRaftelData<TDbContext>(...)`. Health checks cannot depend on a single concrete context type, but the health-check registration API is not generic-friendly at the endpoint layer.
- **No new external dependency**: `Microsoft.Extensions.Diagnostics.HealthChecks` and `Microsoft.AspNetCore.Diagnostics.HealthChecks` ship in the ASP.NET Core shared framework. The project deliberately avoids the `AspNetCore.HealthChecks.*` community catalog.
- **The outbox does not exist yet** (backlog 07, issue #131). `grep -ri outbox src demo tests` returns nothing.
- **Multitenancy does exist** (`Raftel.Infrastructure/Multitenancy`, `ITenantsRepository`), so the tenant check has something real to probe.
- Existing functional-test infrastructure (`tests/Raftel.Api.FunctionalTests`, `ApiTestFactory`) and Testcontainers-based integration tests give a place to assert probe status codes and database-down behavior.

## Goals / Non-Goals

**Goals:**

- Liveness genuinely decoupled from dependencies, so a database outage drains instances instead of restarting them in a loop.
- One registration entry point, `AddRaftelHealthChecks(...)`, that works with any `TDbContext` the application registered.
- Detail exposure that is safe by default: anonymous callers learn only "up / not up".
- Per-check isolation: a slow or hung dependency degrades one entry, not the endpoint.

**Non-Goals:**

- Implementing the outbox itself, or a UI/dashboard for health results.
- Publishing health results to an external monitoring system (`IHealthCheckPublisher`) — an application can add one on top.
- Checks for dependencies the framework does not own (message brokers, caches, third-party HTTP APIs).
- Degraded-status semantics at the HTTP level beyond the standard mapping (degraded stays 200).

## Decisions

### 1. Build on `Microsoft.Extensions.Diagnostics.HealthChecks`, not a hand-rolled abstraction

The shared-framework API already provides registration, tag filtering, per-registration timeout, result aggregation, and `MapHealthChecks`. Writing our own would duplicate it and break orchestrator tooling expectations.

- *Alternative — `AspNetCore.HealthChecks.*` packages*: a large catalog, but an external dependency the project avoids, and we need four checks that are each a handful of lines.
- *Alternative — custom `IRaftelHealthCheck`*: no benefit; `IHealthCheck` is already a minimal interface with no framework coupling worth hiding.

### 2. Checks live in `Raftel.Infrastructure`; endpoints live in `Raftel.Api.Server`; shared options/tag live in `Raftel.Application`

`DatabaseHealthCheck` / `PendingMigrationsHealthCheck` need `DbContext`. Mapping and serialization need `IEndpointRouteBuilder`. Splitting along that line keeps EF out of the API layer, as `.github/instructions/clean-architecture` and the architecture tests require.

`Raftel.Api.Server`'s actual project reference set is `Application` + `Domain` + `Shared` only — it does **not** reference `Infrastructure` (confirmed by the `Api_Should_Not_DependOn_Infrastructure` architecture test), unlike what the top-level `CLAUDE.md`'s prose summary suggests. `HealthEndpointExtensions` needs `HealthOptions` (for the detail endpoint's authorization policy) and the `ready` tag, so both `HealthOptions` and `HealthCheckTags` live in `Raftel.Application/Abstractions/Health/` instead of `Raftel.Infrastructure`. `Raftel.Infrastructure` references `Application` already, so `AddRaftelHealthChecks` binds and consumes the same `HealthOptions` type without issue. The per-check-name constants (`HealthCheckNames`) stay in `Raftel.Infrastructure` since only registration and infrastructure tests need them.

### 3. Resolve the DbContext through a non-generic probe, registered by the generic entry point

`AddRaftelHealthChecks(...)` is called without a type argument, but the checks need the concrete context. Resolution: `AddRaftelData<TDbContext>(...)` also registers a non-generic `IDatabaseProbe` implemented by a `DatabaseProbe<TDbContext>` that wraps `CanConnectAsync` and `GetPendingMigrationsAsync`. The checks depend on `IDatabaseProbe` only.

- *Alternative — `AddRaftelHealthChecks<TDbContext>(...)`*: forces every caller to repeat the context type already supplied to `AddRaftelData`, and makes the API-layer extension generic too.
- *Alternative — resolve `DbContext` base type from DI*: `AddDbContext<TDbContext>` does not register the base `DbContext`, so this would need an extra registration anyway; the probe is the same cost with a clearer contract and a trivial test double.

### 4. Tag-driven endpoints, with liveness deliberately running zero checks

- `/health/live` → predicate `_ => false`: no check runs, so nothing can touch the database. This is the mechanism that satisfies the spec's "liveness independent of dependencies" requirement; it is stronger than "no check is tagged live", because a later check added without tags cannot leak into liveness.
- `/health/ready` → predicate `registration.Tags.Contains("ready")`.
- `/health` → all checks.

Tag constants live in one `HealthCheckTags` type so registrations and endpoint predicates cannot drift.

### 5. Authorization applies to the detail endpoint only, via the standard endpoint metadata

`MapHealthChecks("/health")` gets `.RequireAuthorization(...)` when `HealthOptions.RequireAuthorizationForDetails` is true (default), using `DetailsPolicy` when set and the default policy otherwise. `/health/live` and `/health/ready` get `.AllowAnonymous()` explicitly, so a global fallback authorization policy in a host application cannot accidentally lock out the orchestrator's probes.

Defense in depth: even when an operator disables authorization, the public writer is still the one used for `live` and `ready`, so those two endpoints can never emit per-check detail.

- *Alternative — separate internal port for `/health`*: a valid deployment pattern, but it is a hosting decision, not a framework one; `RequireAuthorizationForDetails` composes with it rather than replacing it.

### 6. Two response writers, not one with a flag threaded through the response

`HealthResponseWriter.WriteDetailed` emits `status`, `totalDurationMs`, `entries` (per check: `status`, `durationMs`, optional `description`). `HealthResponseWriter.WriteStatusOnly` emits `status` and `totalDurationMs` and nothing else. Two explicit writers make the "public endpoints cannot leak detail" property visible at the mapping site and hard to regress.

Descriptions are framework-authored strings only (for example `"34 messages pending older than 00:05:00"`). No writer ever reads `HealthReportEntry.Exception`, and check implementations never place an exception message, connection string, or configuration value into `Description` or `Data`. Serialization uses `System.Text.Json` with the enum written as its name.

### 7. Per-check timeout via `HealthCheckRegistration.Timeout`, plus cooperative cancellation inside each check

Each registration gets `HealthOptions.CheckTimeout`. `HealthCheckService` links the timeout into the token it passes to the check and reports that entry as unhealthy when it elapses. Every check honors the token it is given (EF's `CanConnectAsync(cancellationToken)` etc.), which is what makes the timeout actually cut the work short rather than only shortening the reported wait.

### 8. Checks never throw; they translate failure into a status

`HealthCheckService` already catches exceptions and records them as unhealthy — but it records the exception on the entry, and an entry carrying an exception is one careless writer away from leaking a connection string. Catching inside each check and returning `HealthCheckResult.Unhealthy(description: <safe text>)` keeps the failure detail out of the report object entirely. The exception detail is not lost: it is logged through the structured-logging capability (#128), which is where operators should read it.

### 9. Startup readiness is a singleton flag flipped by a hosted service

`StartupHealthCheck` holds a `volatile bool` and reports `Unhealthy` until set. A `StartupHostedService` runs the application's startup work and sets the flag on success only — a failed startup leaves the instance permanently not ready, which is the correct behavior for a readiness probe (the orchestrator keeps it out of the balancer and the failure is visible).

The framework does not run migrations itself here; it exposes the gate and a hook the application's startup work plugs into. That keeps "who applies migrations" an application decision, which the project already treats as such.

### 10. Optional checks are opted into explicitly, not discovered

`AddRaftelHealthChecks` takes an options callback that also carries opt-ins:

```csharp
services.AddRaftelHealthChecks(options =>
{
    options.CheckTimeout = TimeSpan.FromSeconds(3);
    options.EnableTenantResolutionCheck = true;
    options.EnableOutboxCheck = true;   // requires an IOutboxHealthProbe registration
});
```

Service-provider probing at registration time is order-dependent and silently wrong when the outbox is registered after health checks. An explicit flag fails loudly instead: enabling the outbox check without an `IOutboxHealthProbe` registered throws at startup with a message naming the missing registration.

### 11. `OutboxHealthCheck` ships against `IOutboxHealthProbe`, an abstraction the outbox will implement

The check needs two numbers: the age of the oldest unprocessed message and the dead-letter count. `IOutboxHealthProbe` exposes exactly those, so this change can ship (and unit-test) the threshold logic now without the outbox, and backlog 07 only has to implement the probe. The probe interface lives in `Raftel.Application/Abstractions` next to the other framework abstractions, so the future outbox implementation does not have to reach into a health-specific namespace.

## Risks / Trade-offs

- **An application with a fallback authorization policy locks out its own probes** → `/health/live` and `/health/ready` are mapped with explicit `.AllowAnonymous()`, and a functional test asserts an unauthenticated call to both succeeds while `/health` returns 401.
- **`/health` leaks internal structure to an authenticated but low-privilege user** → the default requires authorization and `DetailsPolicy` lets an operator demand a specific policy; documentation states the endpoint should be treated as an operator surface.
- **A check that ignores its `CancellationToken` still blocks the endpoint** → registration timeout bounds what the service reports, and every framework check honors the token; the documented extension guidance for custom checks repeats this requirement.
- **`PendingMigrationsHealthCheck` on every readiness probe adds a database round trip** → `GetPendingMigrationsAsync` is cheap relative to probe intervals; if it proves costly, the result can be cached once it is empty (a schema cannot regain pending migrations without a restart). Not done now — premature.
- **`EnableOutboxCheck` throwing at startup is a hard failure** → deliberate: silently skipping the check reproduces exactly the invisibility problem the outbox check exists to solve.
- **Liveness returning 200 while the process is deadlocked in a way that still serves requests** → out of scope; liveness by definition can only prove the request pipeline responds.

## Migration Plan

Purely additive. No public type is removed or renamed, so existing applications are unaffected until they opt in.

1. Ship the checks, options, endpoints, and writer; nothing is registered unless the application calls `AddRaftelHealthChecks` and maps the endpoints.
2. Wire the demo app (`demo/Raftel.Api.FunctionalTests.DemoApi/Program.cs`) so the functional tests exercise the real pipeline.
3. Document the three endpoints, their meaning, the security defaults, and example Kubernetes `livenessProbe` / `readinessProbe` snippets in `docs/`.
4. When backlog 07 lands, it implements `IOutboxHealthProbe` and applications flip `EnableOutboxCheck`.

Rollback: remove the `AddRaftelHealthChecks` call and the endpoint mapping — no schema or data change is involved.
