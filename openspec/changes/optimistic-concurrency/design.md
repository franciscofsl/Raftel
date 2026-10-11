## Context

See [proposal.md](proposal.md) — Why. Current state that shapes the approach:

- Entity configurations are `IEntityTypeConfiguration<T>` classes applied with `ApplyConfigurationsFromAssembly`. They receive no `DatabaseProvider`; existing opt-ins (`HasSoftDelete()`, `HasTenantId()`, `HasUserAuditing()`) are parameterless and add shadow properties named in `ShadowPropertyNames`.
- `RaftelDbContext.OnModelCreating` already loops over every entity type to build the combined soft-delete/tenant filter, and it can see the active provider through `Database`.
- `RaftelDbContext.CommitAsync` calls `base.SaveChangesAsync(cancellationToken)` directly. Identity, the audit store and domain event handlers call `SaveChangesAsync` on the context themselves.
- Repositories load aggregates tracked (`EfRepository.GetByIdAsync` → `FirstOrDefaultAsync`), so a shadow value is readable through the change tracker while the request lives.
- Pipeline today (demo): global `Logging → Validation → AuditLog`, then command `Transaction → UnitOfWork → handler`. Middlewares are opt-in and registration order is pipeline order.
- `Error.Conflict` → 409 already exists (`ErrorResults`). `ErrorType` has no "precondition failed" member.
- `QueryEndpointMapper` already derives response headers from a result interface (`IPagedResult` → `X-Total-Count`).
- EF Core 10.0.1, Npgsql provider 10.0.0. `UseXminAsConcurrencyToken()` (used in the backlog sketch) is obsolete in current Npgsql; the supported form is a `uint` property configured with `IsRowVersion()`.
- A database-managed token only protects the read-modify-write window of a single unit of work. Each HTTP request loads the aggregate afresh, so the cross-request scenario in the issue is only caught when the client sends back the version it read. That is why ETags are part of this change rather than a follow-up (confirmed with the user).

## Goals / Non-Goals

**Goals:**

- Conflict detection enforced by the database in the `UPDATE`/`DELETE` itself — no check-then-write race.
- Zero impact on aggregates: no version member in `Raftel.Domain`, no EF types in `Raftel.Application`.
- Same parameterless opt-in ergonomics as `HasSoftDelete()`.
- Handlers need no new code for `If-Match` to be enforced.

**Non-Goals:**

- `[RetryOnConcurrencyConflict]` (backlog 08 §2.5) — deferred.
- Conditional reads (`If-None-Match` → 304) and weak ETags.
- `If-Match` with a list of entity tags.
- Version tracking for child entities, Identity/OpenIddict tables or audit tables.
- A third provider without a native token (plan B in backlog 08 §7: interceptor-incremented counter).

## Decisions

### Shadow token, provider-specific CLR type, resolved in `OnModelCreating`

`HasOptimisticConcurrency()` only marks the entity type with an annotation (`Raftel:OptimisticConcurrency`). `RaftelDbContext.OnModelCreating`, inside its existing per-entity loop, materialises the token for marked root entity types:

| Provider | Shadow property `Version` |
|---|---|
| SQL Server | `byte[]`, `IsRowVersion()` → `rowversion` column |
| PostgreSQL | `uint`, `IsRowVersion()` → system column `xmin`, no schema change |

- *Why not `HasOptimisticConcurrency(DatabaseProvider)` as in the backlog:* configuration classes have no provider to pass; threading one through would change every `IEntityTypeConfiguration` in consuming apps.
- *Why not a `Version` property on the aggregate (`IHasVersion { byte[] }`):* the CLR type differs per provider (`byte[]` vs `uint`), and it puts a persistence concern in the domain. A shadow property keeps both problems inside Infrastructure.
- *Why not a hand-incremented `uint`:* see backlog 08 §7 — relies on every business method remembering it.

### Version is an opaque string outside Infrastructure

Everything above Infrastructure sees the version as an opaque string: Base64 of the 8 `rowversion` bytes on SQL Server, the decimal `xmin` on PostgreSQL. Two small Application abstractions carry it (`Raftel.Application/Abstractions/Concurrency`):

- `IEntityVersionReader` — `string VersionOf(object entity)`; Infrastructure reads the shadow value from the change tracker. Returns `null` for an entity that is not version-tracked or not tracked.
- `IExpectedVersion` — scoped holder for the version the caller expects (`Value`, `Expect(string)`).

Clients must treat the value as opaque; it is not comparable across providers.

### Exception translation in the `SaveChanges` funnel

`RaftelDbContext` overrides `SaveChangesAsync(bool, CancellationToken)` and `SaveChanges(bool)` — the two methods every other overload funnels into — and rethrows `DbUpdateConcurrencyException` as `ConcurrencyConflictException(entityNames, inner)` (`Raftel.Application/Exceptions`). `CommitAsync` is switched from `base.SaveChangesAsync` to `this.SaveChangesAsync` so it goes through the same funnel.

- *Why the context and not `UnitOfWorkMiddleware`:* saves also come from Identity, auditing and domain event handlers; the context is the only place that sees all of them.
- *Why keep an exception at all:* the failure is born inside EF Core, below the `Result` pattern. It is converted at the pipeline edge and never reaches a handler.

### `ConcurrencyMiddleware` sits outside `TransactionMiddleware`

```
Logging → Validation → AuditLog            (global)
  └ ConcurrencyMiddleware                  ← catch → Result.Failure(Concurrency.Conflict)
      └ TransactionMiddleware              ← sees the exception: rollback, rethrow
          └ UnitOfWorkMiddleware           ← SaveChanges throws here
              └ Handler
```

Command-only, both arities, opt-in. It catches `ConcurrencyConflictException` only and returns `Error.Conflict("Concurrency.Conflict", …)` naming the conflicting entity types.

- *Why outside the transaction:* by the time the failed `Result` exists the rollback has already completed, and the middleware also works for apps that do not register `TransactionMiddleware`.
- *Alternative — between Transaction and UnitOfWork:* also correct (a failed result rolls back), but it would miss conflicts raised by saves that happen during commit-time work outside `UnitOfWorkMiddleware`.

### ETag on reads: result interface, like `IPagedResult`

A response that implements `IVersionedResult { string Version { get; } }` gets `ETag: "<Version>"` from `QueryEndpointMapper` when `ConcurrencyOptions.EnableETags` is on and `Version` is non-empty. Handlers fill it with `IEntityVersionReader.VersionOf(entity)`. `Version` also stays in the JSON body (additive). In the framework, the `GetTenant` response implements it; the functional-test demo API gets a versioned GET/PUT pair to exercise the whole flow.

### `If-Match`: ambient expected version, enforced by the database at save

`CommandEndpointMapper`, when ETags are enabled:

1. Parses `If-Match`. Absent or `*` → no expected version. One quoted strong entity tag → `IExpectedVersion.Expect(tag)`. Anything else → 400 Problem Details, command not dispatched.
2. If the command type carries `[RequiresExpectedVersion]` and no expected version resulted → 428, command not dispatched.
3. After dispatch, a failed result with code `Concurrency.Conflict` is rendered as **412** when an expected version was set, and through the normal mapping (409) otherwise.

A new scoped `ExpectedVersionInterceptor` (`SavingChanges`), registered with the other interceptors, applies it: on the first save of the request that contains at least one `Modified`/`Deleted` version-tracked entry, it sets the token's `OriginalValue` on those entries to the decoded expected version and marks the expectation consumed. EF then emits `… WHERE Id = @id AND Version = @expected`; zero rows affected raises the normal concurrency exception. A value that cannot be decoded for the active provider cannot match any row, so the interceptor throws `ConcurrencyConflictException` directly.

- *Why at save rather than compare-on-load:* comparing after load and then writing is a check-then-act race; overriding the original value lets the database do the comparison atomically with the write.
- *Why ambient rather than a property on the command:* commands are immutable records deserialised from the body; an ambient scoped value needs no command or handler change, so existing commands (`EditUser`, `AssignRoleToUser`) honour `If-Match` for free.
- *Why 412 is decided in the mapper instead of a new `ErrorType`:* 409 vs 412 is purely an HTTP distinction (same conflict, different request shape). Keeping it in the API layer avoids growing the domain enum.
- *Why an attribute for 428 instead of a global `RequireIfMatch` flag:* a global flag would answer 428 on every PUT/DELETE, including commands that touch no version-tracked aggregate. The mapper cannot know that; the command author can.
- *Why `*` does not satisfy `[RequiresExpectedVersion]`:* the attribute exists to forbid blind writes, and `*` is a blind write.

### Options surface

`ConcurrencyOptions { bool EnableETags = false }`, configured with `RaftelApplicationBuilder.ConfigureConcurrency(...)`, following `ConfigurePagination` / `ConfigureAuthorization`, and registered as a singleton.

### Audit exclusion

`ShadowPropertyNames.Version` is added to `ChangeSnapshotExtractor.FrameworkShadowProperties`. The existing "updated with zero property changes → no entity change" rule then covers version-only writes.

## Risks / Trade-offs

- [A command that modifies two version-tracked aggregates in one save, sent with `If-Match`] → the single expected version is applied to both, so the second cannot match and the request fails with 412. Fails closed, never silently. Documented: `If-Match` targets commands that change one version-tracked aggregate.
- [`If-Match` on a command that modifies no version-tracked aggregate] → nothing to compare; the header has no effect. Documented; `[RequiresExpectedVersion]` should only be put on commands over version-tracked aggregates.
- [`ConcurrencyMiddleware` not registered] → the exception reaches the global exception middleware and the client gets 500 instead of 409/412. Same opt-in contract as every other middleware; the registration and its position are documented in the Application `CLAUDE.md` and wired in the demo.
- [SQL Server apps need a migration adding `rowversion` to `User`/`Tenant`/`Role`] → additive, auto-populated column, no backfill needed. Called out in `docs/database-configuration.md`.
- [PostgreSQL `xmin` is a 32-bit transaction id that wraps around and changes on any row rewrite] → standard Npgsql practice; a false match needs the same row to be rewritten exactly 2³² transactions apart. Accepted.
- [Npgsql's convention maps a *shadow* `uint` row version to `xmin`] → believed to hold as for CLR properties; the PostgreSQL infrastructure test is written first and proves it. Fallback if it does not: explicit `HasColumnName("xmin").HasColumnType("xid")`.
- [A model built for one provider is not reusable for the other] → already true (EF caches the model per provider services); migrations remain provider-specific as today.
- [Version in response bodies exposes `xmin`/`rowversion`] → opaque and non-sensitive; accepted.

## Migration Plan

1. Ship with ETags off: only version-tracked entities change behaviour (conflict → exception → 409 once the middleware is registered).
2. Consuming apps: add an EF migration (SQL Server: three `rowversion` columns; PostgreSQL: model snapshot only), register `ConcurrencyMiddleware<>` / `<,>` before `TransactionMiddleware`.
3. Optionally enable `EnableETags`, implement `IVersionedResult` on read models, and mark commands `[RequiresExpectedVersion]`.

Rollback: remove the `HasOptimisticConcurrency()` calls and revert the migration; the column drop is lossless.
