## 1. Options and shared primitives

- [x] 1.1 Add `src/Raftel.Infrastructure/Health/HealthOptions.cs`: `RequireAuthorizationForDetails` (default `true`), `DetailsPolicy` (`string?`), `CheckTimeout` (3s), `DatabaseSlowThreshold` (1s), `OutboxLagThreshold` (5min), `DeadLetterThreshold` (10), `EnableTenantResolutionCheck` (default `false`), `EnableOutboxCheck` (default `false`).
- [x] 1.2 Add `src/Raftel.Infrastructure/Health/HealthCheckTags.cs` with the `ready` tag constant and the check-name constants (`database`, `migrations`, `startup`, `tenants`, `outbox`) used by both registration and tests.
- [x] 1.3 Add `IDatabaseProbe` (`CanConnectAsync`, `GetPendingMigrationsAsync`) plus `DatabaseProbe<TDbContext>` in `src/Raftel.Infrastructure/Health/`, and register it from `AddRaftelData<TDbContext>` in `src/Raftel.Infrastructure/DependencyInjection.cs`.

## 2. Database connectivity check

- [x] 2.1 (test) `tests/Raftel.Infrastructure.Tests/Health/DatabaseHealthCheckTests.cs`: healthy on a fast successful connection; degraded when the connection exceeds `DatabaseSlowThreshold` (fake `IDatabaseProbe` + `TimeProvider`); unhealthy when the probe throws, with no exception escaping the check.
- [x] 2.2 Implement `src/Raftel.Infrastructure/Health/DatabaseHealthCheck.cs` against `IDatabaseProbe`, honoring the supplied `CancellationToken` and returning a safe description that never contains the exception message or connection string.
- [x] 2.3 (test) Assert the unhealthy result's `Description`/`Data` contain neither the exception message nor any connection-string fragment.

## 3. Pending migrations check

- [x] 3.1 (test) `tests/Raftel.Infrastructure.Tests/Health/PendingMigrationsHealthCheckTests.cs`: healthy with no pending migrations; unhealthy with at least one pending; unhealthy (not throwing) when the probe fails.
- [x] 3.2 Implement `src/Raftel.Infrastructure/Health/PendingMigrationsHealthCheck.cs`.

## 4. Startup readiness gate

- [x] 4.1 (test) `tests/Raftel.Infrastructure.Tests/Health/StartupHealthCheckTests.cs`: unhealthy before the flag is set, healthy after, and still unhealthy when the startup work failed.
- [x] 4.2 Implement `src/Raftel.Infrastructure/Health/StartupHealthCheck.cs` (singleton, volatile flag) and `StartupHostedService.cs` that runs the application's registered startup work and sets the flag on success only.

## 5. Registration entry point

- [x] 5.1 Add `AddRaftelHealthChecks(this IServiceCollection, Action<HealthOptions>?)` in `src/Raftel.Infrastructure/DependencyInjection.cs` (or a `Health/HealthDependencyInjection.cs` partial): bind `HealthOptions`, register the singleton `StartupHealthCheck` and its hosted service, and register `database`, `migrations`, `startup` with the `ready` tag and `HealthOptions.CheckTimeout` as each registration's timeout.
- [x] 5.2 Register the tenant and outbox checks only when their opt-in flags are set, and throw at startup with a message naming the missing registration when `EnableOutboxCheck` is set without an `IOutboxHealthProbe`.
- [x] 5.3 (test) `tests/Raftel.Infrastructure.Tests/Health/HealthCheckRegistrationTests.cs`: default registration yields exactly the three mandatory checks, all tagged `ready`, each with the configured timeout; opt-in flags add the optional ones; `EnableOutboxCheck` without a probe throws.

## 6. Endpoints and response writer

- [x] 6.1 Add `src/Raftel.Api.Server/Health/HealthResponseWriter.cs` with `WriteStatusOnly` (`status`, `totalDurationMs`) and `WriteDetailed` (adds `entries` keyed by check name with `status`, `durationMs`, optional `description`), serialized as `application/json` with enum names; neither writer reads `HealthReportEntry.Exception`.
- [x] 6.2 (test) Unit tests for both writers: expected JSON shape, `entries` absent from the status-only body, and an entry whose report carries an exception still producing no exception text.
- [x] 6.3 Add `src/Raftel.Api.Server/Health/HealthEndpointExtensions.cs` mapping `/health/live` (predicate `_ => false`, status-only writer, `AllowAnonymous`), `/health/ready` (predicate on the `ready` tag, status-only writer, `AllowAnonymous`), and `/health` (all checks, detailed writer, `RequireAuthorization` per `HealthOptions`).
- [x] 6.4 Confirm `Raftel.Api.Server` gains no `EntityFrameworkCore` reference and the architecture tests still pass.

## 7. Demo wiring

- [x] 7.1 Call `AddRaftelHealthChecks(...)` and map the health endpoints in `demo/Raftel.Api.FunctionalTests.DemoApi/Program.cs`.
- [x] 7.2 Add a test-only switch in the demo app (configuration flag or test-support service) that makes the database probe fail on demand, so functional tests can exercise a database-down readiness response without stopping a container.

## 8. Functional tests

- [x] 8.1 `tests/Raftel.Api.FunctionalTests/HealthCheckTests.cs`: `/health/live` returns 200 with a healthy database and 200 with the database failing, and no database connection is attempted while serving it.
- [x] 8.2 `/health/ready` returns 200 when healthy and 503 when the database probe fails or migrations are pending; 200 with a degraded aggregate status when only a degraded check is present.
- [x] 8.3 Unauthenticated `/health` returns 401 under the default options, with a body carrying no check names or statuses; an authorized call returns the detailed body; an opted-out configuration returns the detailed body anonymously.
- [x] 8.4 `/health/live` and `/health/ready` remain reachable anonymously even with a fallback authorization policy configured.
- [x] 8.5 No health response body contains a stack trace, exception message, or connection-string fragment, at any endpoint, for a failing check.

## 9. Timeout isolation

- [x] 9.1 (test) Register a check that blocks until cancelled and assert the endpoint still responds, that check is reported unhealthy, and the other checks report their own results.

## 10. Outbox check

- [x] 10.1 Add `IOutboxHealthProbe` (oldest unprocessed message age, dead-letter count) to `src/Raftel.Application/Abstractions/`.
- [x] 10.2 (test) `tests/Raftel.Infrastructure.Tests/Health/OutboxHealthCheckTests.cs`: healthy under both thresholds; degraded when the oldest unprocessed message exceeds `OutboxLagThreshold`; unhealthy when dead letters exceed `DeadLetterThreshold`; unhealthy (not throwing) when the probe fails.
- [x] 10.3 Implement `src/Raftel.Infrastructure/Health/OutboxHealthCheck.cs` with a description that reports counts and thresholds only.

## 11. Tenant resolution check

- [x] 11.1 (test) `tests/Raftel.Infrastructure.Tests/Health/TenantResolutionHealthCheckTests.cs`: healthy when the tenant store responds; unhealthy (not throwing) when it fails or times out.
- [x] 11.2 Implement `src/Raftel.Infrastructure/Health/TenantResolutionHealthCheck.cs` against the existing multitenancy abstractions.

## 12. Integration tests

- [x] 12.1 Testcontainers-based integration test in `tests/Raftel.Infrastructure.Tests` (or the integration project, matching the existing multi-provider setup): stop the database container and assert the database check transitions from healthy to unhealthy, then recovers after restart.

## 13. Documentation

- [x] 13.1 Document the three endpoints, their meaning, the security defaults, `HealthOptions`, and example Kubernetes `livenessProbe` / `readinessProbe` snippets in `docs/` (new `docs/health-checks.md`, linked from `docs/database-configuration.md`).
- [x] 13.2 Update `src/Raftel.Infrastructure/CLAUDE.md` and `src/Raftel.Api.Server/CLAUDE.md` with the health-check structure and the rule that custom checks must honor their `CancellationToken` and must never put exception text into a description.

## 14. Verification

- [x] 14.1 Run `dotnet build Raftel.sln` and the full `dotnet test` suite; confirm every new test passes and the architecture tests still hold.
