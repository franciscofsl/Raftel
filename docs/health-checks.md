# Health Checks

Raftel ships liveness, readiness, and diagnostic endpoints on top of
`Microsoft.Extensions.Diagnostics.HealthChecks` — part of the ASP.NET Core
shared framework, so no extra NuGet package is required.

## Endpoints

| Endpoint | Runs | Meaning | Auth |
|---|---|---|---|
| `/health/live` | no checks | the process is responsive | anonymous |
| `/health/ready` | checks tagged `ready` | dependencies are ready to serve traffic | anonymous |
| `/health` | every registered check | full diagnostic detail | authorized by default |

`/health/live` never touches the database or any other dependency: it exists
so an orchestrator can tell "the process is alive" apart from "the process is
ready", and a database outage drains the instance from the load balancer
instead of restarting it in a loop.

`/health` is the only endpoint that reports per-check detail (status,
duration, a short description). `/health/live` and `/health/ready` only ever
return the aggregate status — never a check name, a description, an
exception message, or a connection string.

## Registration

```csharp
services.AddRaftelData<MyDbContext>(configuration);
services.AddRaftelHealthChecks(options =>
{
    options.EnableTenantResolutionCheck = true; // opt-in, off by default
    options.EnableOutboxCheck = true;            // requires an IOutboxHealthProbe registration
});
```

```csharp
app.MapRaftelHealthChecks();
```

`AddRaftelHealthChecks` must run after `AddRaftelData<TDbContext>`, since the
database checks depend on the `IDatabaseProbe` it registers.

## Checks

- **Database connectivity** (`ready`): `Healthy` on a fast connection,
  `Degraded` past `DatabaseSlowThreshold`, `Unhealthy` on failure.
- **Pending migrations** (`ready`): `Unhealthy` while the schema has
  unapplied migrations, so traffic is never served against a stale schema.
- **Startup** (`ready`): `Unhealthy` until the application's startup work
  (register an `IStartupTask`) completes; stays `Unhealthy` forever if that
  work fails, rather than restarting the check.
- **Tenant resolution** (`ready`, opt-in via `EnableTenantResolutionCheck`):
  verifies the tenant store responds.
- **Outbox backlog** (`ready`, opt-in via `EnableOutboxCheck`): `Degraded`
  when the oldest unprocessed message is older than `OutboxLagThreshold`,
  `Unhealthy` when dead letters exceed `DeadLetterThreshold`. Requires an
  `IOutboxHealthProbe` implementation to be registered — enabling the flag
  without one throws at startup.

## Options (`HealthOptions`)

| Option | Default | Meaning |
|---|---|---|
| `RequireAuthorizationForDetails` | `true` | require authorization for `/health` |
| `DetailsPolicy` | `null` | named authorization policy for `/health`; the default policy is used when unset |
| `CheckTimeout` | 3s | per-check timeout; a check that exceeds it is reported `Unhealthy` without blocking the others |
| `DatabaseSlowThreshold` | 1s | connection time above which the database check reports `Degraded` |
| `OutboxLagThreshold` | 5min | oldest unprocessed message age above which the outbox check reports `Degraded` |
| `DeadLetterThreshold` | 10 | dead-letter count above which the outbox check reports `Unhealthy` |
| `EnableTenantResolutionCheck` | `false` | opt in to the tenant check |
| `EnableOutboxCheck` | `false` | opt in to the outbox check |

## Kubernetes probes

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 3
```

## Writing a custom check

A check that plugs into `AddRaftelHealthChecks` (or is registered directly
with `IHealthChecksBuilder`) must:

- honor the `CancellationToken` it is given, so `CheckTimeout` actually cuts
  the work short instead of only shortening how long the caller waits;
- never put an exception message, a stack trace, or a connection string into
  its `Description` or `Data` — catch internally and return
  `HealthCheckResult.Unhealthy("<safe text>")`; the real exception belongs in
  the application's structured logs, not in the health response body.
