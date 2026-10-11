# Tasks

Each implementation task follows TDD: write the named failing test first, then the code that makes it pass.

## 1. Seeding contract and discovery (Raftel.Application)

- [ ] 1.1 Add `IDataSeeder`, `ITenantDataSeeder` and `ISeedRunner` under `Abstractions/Seeding` with `Version`/`RunOnce` as defaulted members; verify `dotnet build src/Raftel.Application` succeeds and a unit test asserts a minimal seeder reports `Version == 1` and `RunOnce == false`
- [ ] 1.2 Add `SeedOptions` (migration, run, fail-fast flags; `AdminEmail`, `AdminPassword`, `AdminRoleName`, `Roles`, `DefaultTenant`); verify a unit test asserts the defaults from design D9 (`ApplyMigrationsOnStartup == false`, `RunSeedersOnStartup == true`, `FailFastOnError == true`, `AdminPassword == null`)
- [ ] 1.3 Extend the assembly scan in `DependencyInjection.RegisterHandlers` to register `IDataSeeder` implementations as scoped; verify a unit test resolves a seeder defined in a scanned test assembly and does not resolve an abstract one
- [ ] 1.4 Add `PermissionSetAttribute`, apply it to `UsersPermissions` and `TenantsPermissions`, and add `IPermissionSetProvider` populated at registration from marked classes; verify unit tests show the provider returns every `const string` of the two built-in sets and ignores an unmarked class with string constants
- [ ] 1.5 Verify `dotnet test tests/Raftel.ArchitectureTests` still passes (Application depends only on Domain)

## 2. Permission catalog (Raftel.Domain + Raftel.Infrastructure)

- [ ] 2.1 Add `PermissionDefinition` aggregate with `Create`, `MarkObsolete`, `Reinstate` and `IPermissionDefinitionsRepository`; verify domain unit tests cover blank-name rejection, flagging obsolete, and reinstating
- [ ] 2.2 Add `PermissionDefinitionConfiguration` (unique index on `Name`, no tenant id, no soft delete), the `DbSet` on `RaftelDbContext`, and the EF repository registered in `AddRaftelData`; verify an infrastructure integration test persists and reloads a definition and a duplicate name violates the unique index

## 3. Seed history and runner (Raftel.Infrastructure)

- [ ] 3.1 Add `SeedHistory` entity, its configuration (unique `(Name, TenantId)`), the `DbSet`, and internal `ISeedHistoryStore`; verify an integration test upserts a record and reads it back by name and tenant
- [ ] 3.2 Implement `SeedRunner.RunAsync` ordering (ascending `Order`, ties by `Name`) with duplicate-name rejection; verify unit tests for orders 300/100/200, for a tie, and for a duplicate name failing before any seeder executes
- [ ] 3.3 Run each seeder in its own DI scope and transaction, committing on success and rolling back on failure result or exception; verify an integration test where seeders 100 and 200 succeed and 300 fails leaves the first two persisted and nothing from the third, and a throwing seeder is reported as failed
- [ ] 3.4 Honour `FailFastOnError`; verify unit tests show later seeders are skipped when enabled and still run when disabled
- [ ] 3.5 Record history on success only and skip `RunOnce` seeders already applied at the current or higher version; verify unit tests for already-applied skip, version bump re-execution, reconciler re-execution, and no record after failure
- [ ] 3.6 Register `ISeedRunner`, `ISeedHistoryStore` and `SeedOptions` (bound from `Raftel:Seed`, delegate-overridable) in `AddRaftelData`; verify a DI unit test resolves `ISeedRunner` and reads a value set under `Raftel:Seed`

## 4. Built-in seeders (Raftel.Infrastructure)

- [ ] 4.1 Implement `DefaultTenantSeeder` (order 50); verify integration tests for created-when-configured, unchanged-when-existing, and no-op when unconfigured
- [ ] 4.2 Implement `PermissionsSeeder` (order 100) adding missing catalog entries; verify integration tests for empty database, a second run adding nothing, and a new permission added without altering existing entries
- [ ] 4.3 Flag permissions absent from code as obsolete with a log entry and reinstate ones that return; verify integration tests assert the entry survives flagged, roles still hold it, the log reports it, and reappearance clears the flag
- [ ] 4.4 Implement `DefaultRolesSeeder` (order 200) creating configured roles, the matching `IdentityRole`, and adding missing permissions without removing extras; verify integration tests for the default `Admin` role holding every non-obsolete permission, a new permission reaching an existing role, a customized extra permission being kept, and an unknown configured permission failing with its name
- [ ] 4.5 Implement `AdminUserSeeder` (order 300) creating the user through `IAuthenticationService` and assigning the administrator role; verify integration tests that the user can log in with the configured password, holds the role, and that a second run creates no second user and leaves the password unchanged
- [ ] 4.6 Enforce credential rules: fail outside Development when password or email is missing; verify a dedicated security unit test asserts a Production run with no password returns a failure naming `Raftel:Seed:AdminPassword` and creates no user
- [ ] 4.7 Generate and log a development-only password once in Development; verify unit tests assert the generated password satisfies the Identity policy, is logged once on creation, is not logged on a second run, and a configured password never appears in captured log output
- [ ] 4.8 Surface Identity password-policy rejection as a failure result; verify a test with a weak configured password fails with the policy description and creates no user
- [ ] 4.9 Register the four seeders in `AddRaftelData` and verify `grep -rn "AdminPassword" --include=appsettings*.json .` finds no value in the repository

## 5. Tenant seeding

- [ ] 5.1 Add the tenant pass to `SeedRunner.RunAsync` (per tenant, per `ITenantDataSeeder`, inside `ICurrentTenant.Change`, own transaction, history per tenant); verify an integration test with two tenants runs the seeder twice with the correct current tenant and writes tenant-owned data, and a failure for the second tenant keeps the first tenant's data
- [ ] 5.2 Implement `ISeedRunner.RunForTenantAsync` in the ambient scope with nested transaction; verify a unit test shows it joins an active transaction rather than opening a new scope
- [ ] 5.3 Raise `TenantCreated` from `Tenant.Create`; verify a domain unit test asserts the event is raised with the new tenant id and existing tenant domain tests still pass
- [ ] 5.4 Add the `TenantCreated` handler calling `RunForTenantAsync` and throwing `SeedingFailedException` on failure; verify integration tests that creating a tenant via the command runs a registered tenant seeder in that tenant's context, and that a failing tenant seeder under `TransactionMiddleware` persists neither tenant nor seeded data

## 6. Startup entry point and readiness

- [ ] 6.1 Add `IDatabaseMigrator` / `DatabaseMigrator<TDbContext>` registered in `AddRaftelData`; verify an integration test applies pending migrations and reports none pending afterwards
- [ ] 6.2 Implement `MigrateAndSeedAsync` per design D11; verify integration tests for migrate-then-seed when enabled, failure with a "migrations pending" message and no seeder executed when disabled, seeders skipped when `RunSeedersOnStartup` is false, and `SeedingFailedException` thrown on failure with fail-fast
- [ ] 6.3 Add `SeedingState` and `SeedingHealthCheck` (tag `ready`) registered in `AddRaftelHealthChecks`; verify unit tests map each state to the status in design D12 and a functional test shows `/health/ready` returns 200 when seeding was never requested
- [ ] 6.4 Verify readiness end to end with a functional test: 503 while a blocking seeder is running, 200 after completion, and 200 with degraded status after a failure with fail-fast disabled
- [ ] 6.5 Document the `seeding` check in `docs/health-checks.md`; verify the documented states match the unit tests from 6.3

## 7. Demo and documentation

- [ ] 7.1 Add `[PermissionSet]` to `PiratesPermissions`, move the OpenIddict client registration into a demo `IDataSeeder`, delete `SeedData`, and call `MigrateAndSeedAsync` from the demo `Program.cs`; verify the demo starts against an empty database and the catalog contains the Pirates, Users and Tenants permissions
- [ ] 7.2 Add the demo migration for `PermissionDefinition` and `SeedHistory`; verify `dotnet ef migrations has-pending-model-changes` reports none for the demo context
- [ ] 7.3 Make the functional-test fixture inject a randomly generated administrator password through configuration and update tests that referenced the lowercase `admin` role; verify `dotnet test tests/Raftel.Api.FunctionalTests` passes
- [ ] 7.4 Add a functional test for the acceptance path: empty database, start, log in as the seeded administrator in host context, call a permission-protected endpoint successfully
- [ ] 7.5 Write `docs/getting-started.md` covering the startup flow, `Raftel:Seed` settings, credential rules, writing a custom seeder and tenant seeder, the required migration, the multi-replica recommendation, and the host-context role limitation; verify every code snippet in it compiles against the demo

## 8. Integration checks

- [ ] 8.1 Run seeding twice against an empty database in an integration test and assert row counts for tenants, catalog permissions, roles, role permissions and users are identical after both runs
- [ ] 8.2 Run `dotnet build Raftel.sln` and `dotnet test`; verify zero errors and all projects green on both SQL Server and PostgreSQL providers
- [ ] 8.3 Run `openspec validate data-seeding-bootstrap` and verify it reports no errors
