## Why

The solution has zero health checks: `grep -r "HealthCheck" src/` returns nothing and no `/health*` endpoint is mapped anywhere. Operationally this means an orchestrator (Kubernetes, App Service) has no way to decide whether an instance should be restarted or pulled out of the load balancer, traffic is admitted before migrations finish, and a database outage is discovered through user-facing 500s rather than through a probe.

Per [GitHub issue #133](https://github.com/franciscofsl/Raftel/issues/133) (backlog item [09](../../../docs/backlog/09-health-checks.md)), the framework should ship the probes itself, built on `Microsoft.Extensions.Diagnostics.HealthChecks` — part of the ASP.NET Core shared framework, so no new external dependency. The distinction between liveness and readiness is the point: a single `/health` endpoint that touches the database would make a database outage restart every pod in a loop instead of simply draining them.

## What Changes

- Add `HealthOptions` (`Raftel.Infrastructure`): `RequireAuthorizationForDetails` (default `true`), `DetailsPolicy`, `CheckTimeout` (3s), `DatabaseSlowThreshold` (1s), `OutboxLagThreshold` (5min), `DeadLetterThreshold` (10).
- Add `AddRaftelHealthChecks(...)` to `Raftel.Infrastructure`'s DI surface, registering the checks below and binding `HealthOptions`.
- Add `DatabaseHealthCheck` (tag `ready`): `CanConnectAsync` under a short timeout; `Degraded` when it exceeds `DatabaseSlowThreshold`, `Unhealthy` when it fails — never throwing out of the check.
- Add `PendingMigrationsHealthCheck` (tag `ready`): `Unhealthy` when `GetPendingMigrationsAsync()` is non-empty, so traffic is not served against a stale schema.
- Add `StartupHealthCheck` (tag `ready`): an in-memory readiness flag flipped by a startup `IHostedService`, keeping the app "not ready" until startup work (migrations) completes.
- Add `TenantResolutionHealthCheck` (tag `ready`, opt-in): verifies the tenant store responds, using the existing multitenancy abstractions.
- Add `OutboxHealthCheck` (tag `ready`, registered only when an outbox is present): `Degraded` when unprocessed messages are older than `OutboxLagThreshold`, `Unhealthy` when dead letters exceed `DeadLetterThreshold`. The outbox itself does not exist yet (backlog [07](../../../docs/backlog/07-outbox-domain-events.md), issue #131), so this change introduces the check against a small `IOutboxHealthProbe` abstraction that the outbox will implement later; with no implementation registered, the check is not registered either.
- Map three endpoints in `Raftel.Api.Server`: `/health/live` (no checks — process liveness only, never touches the database), `/health/ready` (tag `ready`), `/health` (all checks, detailed).
- Add a JSON `HealthResponseWriter`: `status`, `totalDurationMs`, and per-entry `status`/`durationMs`/`description`. `/health/live` and `/health/ready` return status only, anonymously; `/health` returns the detailed body and requires authorization by default. Exception messages, stack traces, and connection strings never reach any response body.
- Apply a per-check timeout so one hung check cannot hang the endpoint.
- Wire the endpoints into the demo app and document example Kubernetes probes.

Breaking public API: **no** — purely additive.

## Capabilities

### New Capabilities
- `health-checks`: liveness/readiness/diagnostic probe endpoints, the individual checks (database, pending migrations, startup, tenant resolution, outbox), their healthy/degraded/unhealthy semantics, per-check timeout isolation, the JSON response format, and the security rules governing what each endpoint may expose and to whom.

### Modified Capabilities
_None — no existing spec under `openspec/specs/` changes; the demo app and `DependencyInjection` are wiring, not spec-level behavior._

## Impact

- **New**: `src/Raftel.Application/Abstractions/Health/{HealthOptions,HealthCheckTags}.cs`, `src/Raftel.Application/Abstractions/IOutboxHealthProbe.cs`, `src/Raftel.Infrastructure/Health/{DatabaseHealthCheck,PendingMigrationsHealthCheck,StartupHealthCheck,StartupHostedService,IStartupTask,TenantResolutionHealthCheck,OutboxHealthCheck,IDatabaseProbe,DatabaseProbe,HealthCheckNames,HealthDependencyInjection}.cs`, `src/Raftel.Api.Server/Health/{HealthEndpointExtensions,HealthResponseWriter}.cs`, `tests/Raftel.Infrastructure.Tests/Health/**`, `tests/Raftel.Api.FunctionalTests/HealthCheckTests.cs`.
- **Modified**: `src/Raftel.Infrastructure/DependencyInjection.cs`, `demo/Raftel.Api.FunctionalTests.DemoApi/Program.cs`, `docs/database-configuration.md` (or a new `docs/health-checks.md`).
- **Dependencies**: `Microsoft.Extensions.Diagnostics.HealthChecks` / `Microsoft.AspNetCore.Diagnostics.HealthChecks` from the ASP.NET Core shared framework — no new NuGet package. `Raftel.Api.Server` must not gain an `EntityFrameworkCore` reference: the EF-touching checks live in `Raftel.Infrastructure`.
- **Depends on**: structured logging / correlation ([#128](https://github.com/franciscofsl/Raftel/issues/128), merged as [#148](https://github.com/franciscofsl/Raftel/pull/148)) for probe failures to be diagnosable.
- **Blocks**: [#134](https://github.com/franciscofsl/Raftel/issues/134), [#138](https://github.com/franciscofsl/Raftel/issues/138), [#139](https://github.com/franciscofsl/Raftel/issues/139).
- **Related, not blocking**: the outbox ([#131](https://github.com/franciscofsl/Raftel/issues/131)) — `OutboxHealthCheck` ships against an abstraction and activates once the outbox exists.
