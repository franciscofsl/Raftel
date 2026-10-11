# Proposal

## Why

The framework ships an authorization model (`Role`, `Permission`, `UsersPermissions`, `TenantsPermissions`) but nothing initializes it. Against an empty database there is no role, no persisted permission, no administrator — and therefore no way to grant permissions through the API (chicken-and-egg) — and no default tenant. Every consumer ends up writing an ad hoc SQL script, and the demo carries its own hand-rolled `SeedData.InitializeAsync` that calls `Database.MigrateAsync()` and builds an admin role inline.

Per [GitHub issue #134](https://github.com/franciscofsl/Raftel/issues/134) (backlog item [10](../../../docs/backlog/10-seeding-y-bootstrap.md)), the framework should own bootstrap: ordered, idempotent, individually transactional seeders, with safe handling of the initial administrator credentials. Its two prerequisites are now in place: explicit transactions (#129) and health checks (#133).

## What Changes

- Add a seeding contract in `Raftel.Application`: `IDataSeeder` (`Order`, `Name`, `Version`, `RunOnce`, `SeedAsync` returning `Result`), `ITenantDataSeeder` (adds `SeedForTenantAsync`), `ISeedRunner`, and `SeedOptions` bound from the `Raftel:Seed` configuration section.
- Auto-discover consumer seeders in the assemblies already registered through `AddRaftelApplication`, alongside handlers and validators.
- Add `SeedRunner` (`Raftel.Infrastructure`): runs seeders in ascending `Order`, each in its own scope and transaction, records outcomes in a new `SeedHistory` table, and honours `FailFastOnError`.
- Add a standalone permission catalog: new `PermissionDefinition` aggregate and table (name, description, obsolete flag). `Role` and its child `Permission` entities are unchanged.
- Add a `[PermissionSet]` marker attribute; apply it to `UsersPermissions` and `TenantsPermissions`. Consumers mark their own permission classes.
- Add four built-in seeders:
  - `DefaultTenantSeeder` (order 50) — creates the configured default tenant; no-op when none is configured.
  - `PermissionsSeeder` (order 100) — reconciles the catalog with every `const string` in `[PermissionSet]` classes; permissions gone from code are flagged obsolete, never deleted.
  - `DefaultRolesSeeder` (order 200) — creates configured roles (default: one `Admin` role holding every catalog permission) and adds missing permissions to existing ones.
  - `AdminUserSeeder` (order 300) — creates the initial administrator and assigns the administrator role.
- Enforce administrator credential rules: no default password anywhere; password read from `Raftel:Seed:AdminPassword`; missing password outside Development aborts startup; in Development a random password is generated and logged once.
- Add `await app.MigrateAndSeedAsync()` (extension on `IHost`), optionally applying migrations (`ApplyMigrationsOnStartup`, default `false`) before running seeders.
- Add a `seeding` readiness check so `/health/ready` reports not ready while seeding runs or after it fails.
- Raise a `TenantCreated` domain event from `Tenant.Create`; an in-process handler runs tenant seeders for the new tenant. Migration to the outbox waits for #131.
- Replace the demo's `SeedData` with the framework seeders plus one demo `IDataSeeder` for the OpenIddict client; write `docs/getting-started.md` (currently a bare heading).

No **BREAKING** changes to public API. Consumers must add an EF Core migration for the two new tables.

### Out of scope

- `MustChangePassword` on the seeded administrator: neither `User` nor the Identity setup has the concept, and there is no change-password flow. Deferred to backlog item 21.
- Per-tenant built-in seeders: default roles and the administrator are seeded once in host context (no tenant). `ITenantDataSeeder` is an extension point for consumers only.
- Outbox-backed delivery of `TenantCreated` (#131).
- Distributed locking across replicas seeding concurrently.

## Capabilities

### New Capabilities

- `data-seeding`: Ordered, idempotent, individually transactional bootstrap of initial data at application startup — seeder contract and discovery, execution history, permission-catalog reconciliation, default roles, initial administrator and its credential rules, default tenant, per-tenant seeding, optional migration application, and readiness gating.

### Modified Capabilities

None. `openspec/specs/` holds no archived capabilities yet; the readiness behaviour added here is specified inside `data-seeding` rather than as a delta to the still-unarchived `health-checks` change.

## Impact

- **New code**: `src/Raftel.Application/Abstractions/Seeding/**`, `src/Raftel.Application/SeedOptions.cs`, `src/Raftel.Application/Authorization/PermissionSetAttribute.cs`, `src/Raftel.Domain/Features/Authorization/PermissionDefinition*.cs`, `src/Raftel.Domain/Features/Tenants/TenantCreated.cs`, `src/Raftel.Infrastructure/Seeding/**`, EF configurations and repository for the two new tables.
- **Modified code**: `Raftel.Application/DependencyInjection.cs` (seeder scan), `Raftel.Infrastructure/DependencyInjection.cs` (seeding services), `Raftel.Infrastructure/Health/HealthDependencyInjection.cs` (`seeding` check), `RaftelDbContext` (two `DbSet`s), `Tenant.Create` (raises event), `UsersPermissions`/`TenantsPermissions` (attribute), demo `Program.cs` and `SeedData`.
- **Database**: two additive tables (`PermissionDefinition`, `SeedHistory`); a new demo migration.
- **Configuration**: new `Raftel:Seed` section. `AdminPassword` must come from environment, user-secrets or a vault — never a committed file.
- **Dependencies**: none added.
- **Docs**: `docs/getting-started.md` written; `docs/health-checks.md` gains the `seeding` check.
- **Blocks**: #138.
