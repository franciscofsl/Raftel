## Why

Nothing in the model carries a concurrency token: `Entity<TId>` only has `Id`, and no EF configuration declares `IsRowVersion()` or `IsConcurrencyToken()`. Two callers who read the same aggregate and then each save an edit silently overwrite one another (lost update) — the second write wins and the first caller is never told. For a framework that ships `EditUser`, `AssignRoleToUser` and multitenancy this is a real data-integrity gap, and the prerequisites to close it cleanly (typed `Error` → HTTP mapping, #125; explicit transactions, #129) are now in place.

## What Changes

- Add an opt-in, per-entity optimistic concurrency token, declared with `HasOptimisticConcurrency()` next to the existing `HasSoftDelete()` / `HasTenantId()`. The token is a database-managed shadow property (`rowversion` on SQL Server, `xmin` on PostgreSQL) — the aggregate is not touched and nothing is incremented by hand.
- Enable it on the framework's own `User`, `Tenant` and `Role` aggregates.
- Translate EF Core's `DbUpdateConcurrencyException` into an EF-free `ConcurrencyConflictException` at the `DbContext` boundary, so no EF type leaks into the application layer.
- Add `ConcurrencyMiddleware<TRequest>` / `<TRequest, TResult>` — command-only, opt-in at registration — which turns that exception into `Result.Failure(Error.Conflict("Concurrency.Conflict", …))`, i.e. **HTTP 409** through the existing error mapping.
- Exclude the token from audit logs, like the other framework shadow properties.
- Add opt-in HTTP ETag support in AutoEndpoints (`ConcurrencyOptions.EnableETags`, default off):
  - query results that expose a version are returned with an `ETag` header;
  - command endpoints honour `If-Match`: the supplied version is compared atomically by the database on write, and a mismatch answers **412 Precondition Failed**;
  - a command marked `[RequiresExpectedVersion]` answers **428 Precondition Required** when `If-Match` is missing.
- Document the feature and the schema impact in `docs/database-configuration.md`.

Not breaking: entities without `HasOptimisticConcurrency()` behave exactly as today, the middleware is opt-in, and ETags are off by default. Apps consuming the framework need a migration for `User` / `Tenant` / `Role` on SQL Server (new `rowversion` column); PostgreSQL needs no schema change.

Deliberately out of scope (backlog 08 §2.5): `[RetryOnConcurrencyConflict]`. It is deferred to a follow-up change.

## Capabilities

### New Capabilities
- `optimistic-concurrency`: detection and reporting of conflicting concurrent writes to the same aggregate — opt-in per entity, provider-independent behaviour, conflict surfaced as a typed result (409), token kept out of audit logs, and end-to-end version exchange over HTTP (`ETag` / `If-Match`, 412 / 428).

### Modified Capabilities
<!-- None: openspec/specs/ has no existing capability whose requirements change. -->

## Impact

- `src/Raftel.Application`: new `Exceptions/ConcurrencyConflictException.cs`, `Middlewares/ConcurrencyMiddleware.cs`, `ConcurrencyOptions.cs`, `Authorization/RequiresExpectedVersionAttribute.cs`, `Abstractions/Concurrency/` (`IVersionedResult`, `IEntityVersionReader`, `IExpectedVersion`); `RaftelApplicationBuilder.ConfigureConcurrency`; `GetTenant` response exposes its version.
- `src/Raftel.Infrastructure`: `ShadowPropertyNames.Version`, `HasOptimisticConcurrency()`, `RaftelDbContext` (provider-specific token mapping, exception translation, expected-version enforcement), `User` / `Tenant` / `Role` configurations, `ChangeSnapshotExtractor` (audit exclusion), DI registrations.
- `src/Raftel.Api.Server`: `QueryEndpointMapper` (ETag header), `CommandEndpointMapper` (`If-Match`, 412, 428).
- `demo/`: register `ConcurrencyMiddleware` ahead of `TransactionMiddleware`; new migration; a versioned demo endpoint pair for the functional tests.
- `Raftel.Domain` is not changed: the backlog sketch's `IHasVersion { byte[] Version }` is replaced by an opaque string version at the application edge (see design).
- Tests: unit (`Raftel.Application.UnitTests`), infrastructure on SQL Server + PostgreSQL (`Raftel.Infrastructure.Tests`), integration (`Raftel.Application.IntegrationTests`), functional (`Raftel.Api.FunctionalTests`).
- Tracks GitHub issue #132 / `docs/backlog/08-concurrencia-optimista.md`. Unblocks #138.
