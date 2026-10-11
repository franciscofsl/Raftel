# Design

## Context

See [proposal.md](proposal.md) for motivation. Facts about the current code that shape the approach:

- **`Permission` is a child of `Role`**, not a catalog. `PermissionConfiguration` gives it a required `RoleId` shadow FK and a unique `(RoleId, Name)` index. There is no table listing "all permissions that exist". The backlog's "synchronize with the `Permissions` table" has nothing to synchronize with.
- **Role permissions are not persisted today.** `Role` keeps them in a private `PermissionCollection` that EF never maps; `RoleConfiguration` only declares a navigation-less `HasMany<Permission>().WithMany()` (a `PermissionRole` join table nothing writes) while `Permission.RoleId` is typed `Guid` and so cannot even point at `Role`. Verified empirically on PostgreSQL: a reloaded `Role` has no permissions, so `ClaimsPrincipalFactory` issues no permission claims from the database.
- **Permission classes are plain static classes** of `const string` (`UsersPermissions`, `TenantsPermissions`, demo `PiratesPermissions`). Nothing marks them.
- **Authorization resolves through Identity role names.** `ClaimsPrincipalFactory` reads `userManager.GetRolesAsync` and looks up `Role` aggregates by name; `AuthenticationService.AssignRoleAsync` calls `userManager.AddToRoleAsync`, which requires a matching `IdentityRole` row. Nothing in `src/` creates `IdentityRole` rows today.
- **`Role` is tenant-scoped** (`HasTenantId`); the global query filter hides rows whose `TenantId` differs from the current tenant once one is set. `Tenant` itself has no tenant id and no "active" flag.
- **Explicit transactions exist** (#129): `IUnitOfWork.BeginTransactionAsync` opens a physical transaction or a no-op nested one when a transaction is already active.
- **Health checks exist** (#133): `/health/ready` aggregates checks tagged `ready`; degraded still returns 200. `StartupHostedService` swallows startup-task exceptions and merely stays not ready.
- **Domain events are in-process**, dispatched by `DomainEventsDispatchInterceptor` after `SaveChanges` completes, inside any transaction the command pipeline opened. The outbox (#131) is planned, not built.
- **Assembly scanning** already exists in `Raftel.Application/DependencyInjection.RegisterHandlers` over `builder.Assemblies`.
- The demo's `SeedData.InitializeAsync` migrates, registers an OpenIddict client and builds an `admin` role inline.

Constraints: layer rule (Application depends only on Domain; EF Core forbidden outside Infrastructure), `Result` for business failures, no new external dependencies, no public API break.

## Goals / Non-Goals

**Goals:**

- One call (`MigrateAndSeedAsync`) takes an empty, migrated database to a usable system.
- Seeders are safe to re-run and fail independently.
- No path produces an administrator with a default, empty or silently generated production password.
- Consumers add seeders and permission classes with no registration ceremony.

**Non-Goals:**

- Refactoring `Permission` out of `Role` (the child entity stays; only its mapping is fixed, D15).
- `MustChangePassword` (backlog 21).
- Built-in per-tenant seeders; per-tenant default roles.
- Outbox delivery of `TenantCreated` (#131).
- Distributed locking for concurrent replicas.
- Removing permissions from roles, or deleting catalog entries.

## Decisions

### D1. Permission catalog is a new aggregate, `PermissionDefinition`

New aggregate in `Raftel.Domain/Features/Authorization` with `Name`, `Description`, `IsObsolete` and intent methods `MarkObsolete()` / `Reinstate()`, plus `IPermissionDefinitionsRepository`. Own table, unique index on `Name`, **not** tenant-scoped and **not** soft-deleted: the catalog mirrors code, which is global.

*Alternatives:* (a) no catalog, reconcile only the admin role — loses the obsolete flag and gives `DefaultRolesSeeder` nothing to validate against; (b) make `Permission` a shared entity — breaks the existing schema and the `Role` aggregate boundary, contradicting "no public API break". Confirmed with the user: new catalog table.

### D15. Role permissions are mapped through the existing `Permission` child entity

`Role` keeps a private `List<Permission>` that is the single store; the internal `PermissionCollection` becomes a view over that list (it takes the list in its constructor and is created lazily), so `AddPermission`, `HasPermission`, `PermissionNames` and the rest of the domain API behave exactly as before. `RoleConfiguration` maps the list as a one-to-many to `Permission` with required FK `RoleId` and cascade delete, and marks the navigation `AutoInclude()` so every load of a role (including `ClaimsPrincipalFactory`'s) brings its permissions. `PermissionConfiguration` types the `RoleId` shadow property as `RoleId` (with the same `Guid` conversion as `Role.Id`) so it can be a foreign key, and sets `ValueGeneratedNever()` on `Permission.Id`, because `Permission` ids are assigned in the domain and EF would otherwise treat a permission discovered through a tracked role as `Modified` instead of `Added`.

`Permission` also stops being soft-deleted (`HasSoftDelete` removed from its configuration). The audit interceptor turns every delete into `IsDeleted = true`, so a removed permission would stay in the table and the unique `(RoleId, Name)` index would then reject granting that permission to the role again (found by the default-roles reconciliation test). A permission grant has no history worth keeping; removing it deletes the row.

Schema effect for consumers: `PermissionRole` join table dropped, FK `Permission.RoleId -> Role.Id` added, `Permission.IsDeleted` column dropped. No data migration is needed because nothing could ever write any of these rows.

*Alternatives:* (a) make `PermissionCollection` implement `ICollection<Permission>` and map it directly — larger domain change for no gain; (b) explicit `Include` in `RolesRepository` — every other loader of `Role` (seeders, `ClaimsPrincipalFactory`, handlers) would have to remember it; (c) leave unfixed and seed only the catalog — rejected with the user, the acceptance path (administrator holds every permission) would be unmet. Chosen with the user: fix inside this change.

### D2. Permission classes are marked with `[PermissionSet]`

`PermissionSetAttribute` in `Raftel.Application/Authorization`, applied to `UsersPermissions` and `TenantsPermissions`. Discovery reads public `const string` fields of marked classes in the assemblies registered via `AddRaftelApplication` plus `Raftel.Application` itself. Discovery happens once at registration and is exposed as a singleton `IPermissionSetProvider` (list of names), so the seeder does no reflection and is trivially testable.

*Alternatives:* naming convention (`*Permissions`) — silently picks up unrelated classes; marker interface — impossible on static classes. Name `PermissionSet` avoids colliding with the domain's internal `PermissionCollection`.

### D3. Seeder contract carries `Version` and `RunOnce` as defaulted members

```csharp
public interface IDataSeeder
{
    int Order { get; }
    string Name { get; }
    int Version => 1;
    bool RunOnce => false;
    Task<Result> SeedAsync(CancellationToken cancellationToken = default);
}

public interface ITenantDataSeeder : IDataSeeder
{
    Task<Result> SeedForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
```

The backlog references `RunOnce` and `SeedHistory.Version` without placing them on the interface. Default interface members keep the common reconciling seeder a three-member implementation. A run-once seeder is skipped when history holds `(Name, TenantId)` at `Version >= current`.

All four built-in seeders are reconcilers (`RunOnce = false`).

### D4. `SeedHistory` is an Infrastructure-only table

Plain EF entity `SeedHistory(Name, TenantId?, Version, AppliedOn)` with a unique index on `(Name, TenantId)`, upserted on each successful execution. Not a domain concept, so it lives in `Raftel.Infrastructure/Seeding` behind an internal `ISeedHistoryStore`. Not tenant-filtered (it uses an explicit `TenantId` column rather than the shadow property so the global filter does not hide rows from the runner).

### D5. Startup run: one DI scope and one transaction per seeder

`SeedRunner.RunAsync` resolves seeder *descriptors* (type, order, name) once, then for each seeder creates a fresh `IServiceScope`, resolves the seeder, opens `IUnitOfWork.BeginTransactionAsync`, calls `SeedAsync`, and on success writes history, calls `CommitAsync` on the unit of work and commits the transaction; on failure or exception it rolls back.

A fresh scope per seeder matters as much as the transaction: a failed seeder leaves entities in the `DbContext` change tracker, and a shared context would flush them with the next seeder's commit. `UserManager` operations resolve the same scoped `DbContext`, so Identity writes join the seeder's transaction.

*Alternative:* shared scope with change-tracker reset — depends on EF internals leaking into the runner.

### D6. Tenant pass and `RunForTenantAsync`

After the global pass, `RunAsync` lists tenants (tenant filter irrelevant — `Tenant` is not tenant-scoped) and, per tenant and per `ITenantDataSeeder` in order, runs `SeedForTenantAsync` inside `ICurrentTenant.Change(tenantId)` with the same scope-and-transaction isolation as D5. "Active tenants" in the backlog becomes "all non-deleted tenants" since `Tenant` has no active flag.

`ISeedRunner.RunForTenantAsync(tenantId)` is the runtime entry point. It runs in the **ambient** scope and unit of work: `BeginTransactionAsync` nests into the command's transaction when one is active, so a failing tenant seeder rolls back the tenant creation too.

### D7. New tenants are seeded through an in-process `TenantCreated` event

`Tenant.Create` raises `TenantCreated(TenantId)`. A handler in `Raftel.Application/Features/Tenants/CreateTenant` calls `ISeedRunner.RunForTenantAsync` and throws `SeedingFailedException` on a failure result (domain event handlers return `Task`, not `Result`; an exception here is the infrastructure signal that rolls the transaction back).

Because the interceptor dispatches after `SaveChanges`, the tenant row already exists inside the open transaction when seeders run. Without `TransactionMiddleware` in the pipeline there is no enclosing transaction: the tenant persists and a seeder failure leaves it unseeded until the next startup run reconciles it. Documented.

*Alternatives:* direct call from `CreateTenantCommandHandler` — couples the handler to seeding and has to be undone for #131; defer entirely to #131 — leaves runtime-created tenants unseeded until restart. Confirmed with the user: in-process event now.

### D8. Built-in seeders live in Infrastructure and run in host context

`DefaultTenantSeeder`, `PermissionsSeeder`, `DefaultRolesSeeder`, `AdminUserSeeder` in `Raftel.Infrastructure/Seeding/Seeders`, registered explicitly by `AddRaftelData` (they are not in a consumer-scanned assembly). They run with no current tenant, so roles and the administrator carry a null `TenantId`. Confirmed with the user.

`DefaultRolesSeeder` also ensures an `IdentityRole` with the same name exists via `RoleManager<IdentityRole>`, because `AssignRoleAsync` goes through `AddToRoleAsync`. Role lookup is by name, case-insensitive, to avoid a second role when casing differs.

`AdminUserSeeder` reuses `IAuthenticationService.RegisterAsync` / `AssignRoleAsync` and `User.AssignRole`, mirroring `RegisterUserCommandHandler`, rather than talking to `UserManager` directly.

### D9. `SeedOptions` shape

Bound from `Raftel:Seed`, overridable by delegate:

| Setting | Default | Notes |
|---|---|---|
| `ApplyMigrationsOnStartup` | `false` | Multi-replica race; recommended path is a separate migration job |
| `RunSeedersOnStartup` | `true` | |
| `FailFastOnError` | `true` | |
| `AdminEmail` | none | Required outside Development; Development falls back to `admin@raftel.local` |
| `AdminPassword` | none | Never defaulted; see D10 |
| `AdminRoleName` | `Admin` | |
| `Roles` | one `AdminRoleName` role with `*` | Each: `Name`, `Description`, `Permissions`; `*` means every non-obsolete catalog permission |
| `DefaultTenant` | none | `Name`, `Code`, `Description`; absent means `DefaultTenantSeeder` is a no-op |

There is no "single-tenant mode" switch in the framework, so the default tenant is opt-in by configuration rather than inferred.

### D10. Administrator password rules

- Configured: used as-is, never logged. Identity policy rejection becomes a failure `Result` carrying Identity's description.
- Missing, environment not Development: failure `Result` naming `Raftel:Seed:AdminPassword`. With fail-fast this aborts startup.
- Missing, Development, user absent: generate with `RandomNumberGenerator`, guaranteed to satisfy the configured Identity policy, log once at warning level tagged development-only.
- User already exists: return success before reading or generating any password. This is what makes "logged exactly once" hold across restarts.

Environment comes from `IHostEnvironment`, not from a `SeedOptions` flag, so a misconfigured option cannot unlock generation in production.

### D11. Entry point and failure signalling

`MigrateAndSeedAsync(this IHost)` in `Raftel.Infrastructure/Seeding/HostSeedingExtensions`:

1. If `ApplyMigrationsOnStartup`, migrate through a new scoped `IDatabaseMigrator` (`DatabaseMigrator<TDbContext>`, registered next to `IDatabaseProbe`).
2. Otherwise, if migrations are pending, fail without running seeders — a clear message instead of a "table does not exist" stack trace from the first seeder.
3. If `RunSeedersOnStartup`, run `ISeedRunner.RunAsync`.
4. On failure with `FailFastOnError`, throw `SeedingFailedException`. Startup abort is not business flow, so an exception is appropriate here; seeders themselves still return `Result`.

*Alternative:* implement seeding as an `IStartupTask`. Rejected: `StartupHostedService` swallows exceptions, which contradicts fail-fast aborting the process.

### D12. Readiness through a `seeding` check

Singleton `SeedingState` with states `NotRequested`, `Running`, `Completed`, `CompletedWithFailures`, `Failed`. `SeedingHealthCheck` (tag `ready`) maps them to Healthy, Unhealthy, Healthy, Degraded, Unhealthy. Registered unconditionally by `AddRaftelHealthChecks`; `SeedingState` is `TryAddSingleton` from both `AddRaftelData` and `AddRaftelHealthChecks` so either works alone.

When `MigrateAndSeedAsync` is awaited before `app.Run()` the server is not listening yet, so readiness is trivially false; the check exists for consumers who start seeding concurrently with the host, and to expose `CompletedWithFailures` on `/health/details`.

### D13. Seeder discovery reuses the handler scan

`RegisterHandlers` gains a third per-type step registering non-abstract `IDataSeeder` implementations as scoped services under `IDataSeeder`. Duplicate names are detected by the runner at execution, not at registration, so the error is a `Result` reported through the normal path.

### D14. Demo and docs

Demo `SeedData` is removed. `Program.cs` calls `await app.MigrateAndSeedAsync()` with `ApplyMigrationsOnStartup = true`; the OpenIddict `web-app` client becomes a demo `IDataSeeder`, doubling as the documented example of a consumer seeder. `PiratesPermissions` gets `[PermissionSet]`. The functional-test fixture injects a randomly generated administrator password through configuration; nothing is committed. `docs/getting-started.md` documents the full startup flow.

## Risks / Trade-offs

- **Host-level roles are invisible inside a tenant context** — the tenant query filter hides rows with null `TenantId` once a tenant is set, so an administrator authenticating with a tenant selected resolves no permissions. → Pre-existing filter behaviour, not introduced here; documented in getting-started, and the per-tenant role story is left to a follow-up. A task verifies the seeded administrator authenticates and is authorized in host context.
- **Concurrent replicas seeding at once** → Unique indexes (`PermissionDefinition.Name`, `SeedHistory(Name, TenantId)`, `Tenant.Code`) make the loser fail rather than duplicate; with fail-fast the instance restarts and finds the work done. Docs recommend a single seeding job for multi-replica deployments.
- **Consumers must add a migration** for two new tables → Called out in the proposal and getting-started; the pending-migrations check and D11 step 2 turn a forgotten migration into a clear message.
- **`Tenant.Create` now raises an event** — every tenant creation dispatches `TenantCreated`, including in existing tests → With no tenant seeders registered the handler is a no-op; existing tenant tests are re-run as part of the tasks.
- **Role-permission mapping fix changes the schema** (`PermissionRole` dropped, FK added, `Permission.IsDeleted` dropped) for consumers that already migrated → Nothing could write either table, so the migration is lossless; called out in getting-started. A persistence test locks the behaviour.
- **`AutoInclude` loads permissions on every `Role` query**, including `ListPagedAsync` counts → Role permission sets are small; accepted.
- **Demo role renamed** from lowercase `admin` to `Admin` → Functional tests referencing the role name are updated in the same task group.
- **Development password in logs** → Only in Development, only when the user is created, tagged explicitly; never for a configured password.
- **`FailFastOnError = false` lets a half-seeded app serve traffic** → Surfaces as degraded on readiness and in `/health/details`; default remains fail-fast.
- **Generated password must satisfy a consumer-customized Identity policy** → Generator reads `IdentityOptions.Password`; if Identity still rejects it the seeder fails with Identity's message rather than retrying blindly.

## Migration Plan

1. Upgrade the package; add `[PermissionSet]` to consumer permission classes.
2. Add an EF Core migration (creates `PermissionDefinition` and `SeedHistory`).
3. Provide `Raftel:Seed:AdminEmail` and `Raftel:Seed:AdminPassword` through environment, user-secrets or a vault.
4. Call `await app.MigrateAndSeedAsync()` before `app.Run()`.

Existing databases: the first run adds the catalog, creates or tops up the administrator role (existing role permissions are kept), and creates the administrator only if that email is absent. Rollback: stop calling `MigrateAndSeedAsync`; the two tables are additive and can remain.

## Open Questions

- Whether `TenantCreated` should move to the outbox with at-least-once delivery once #131 lands — decided there, not here.
