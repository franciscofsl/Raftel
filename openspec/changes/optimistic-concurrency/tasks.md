## 1. Reproduce the lost update and add the token

- [ ] 1.1 (test) Add `tests/Raftel.Infrastructure.Tests/Data/ConcurrencyTests.cs`: two `DbContext` instances load the same `Tenant`, both change and save. Verify it currently passes with the second write overwriting the first (documents the bug), on the SQL Server and PostgreSQL fixtures.
- [ ] 1.2 Add `ShadowPropertyNames.Version` and a parameterless `HasOptimisticConcurrency<TEntity>()` in `IEntityTypeConfigurationExtensions.cs` that marks the entity with the `Raftel:OptimisticConcurrency` annotation; verify with a model test that the annotation is present after configuration.
- [ ] 1.3 In `RaftelDbContext.OnModelCreating`, materialise the shadow token for marked root entity types — `byte[]` + `IsRowVersion()` on SQL Server, `uint` + `IsRowVersion()` on PostgreSQL; verify with a model test per provider that the property exists, is a concurrency token, and maps to `xmin` on PostgreSQL (apply the explicit `xmin`/`xid` fallback from design.md if the convention does not map it).
- [ ] 1.4 Call `HasOptimisticConcurrency()` in `UserConfiguration`, `TenantConfiguration` and `RoleConfiguration`; flip 1.1 so the second save must throw `DbUpdateConcurrencyException`, add the same case for `User` and `Role`, and verify green on both providers.
- [ ] 1.5 (test) Add cases: sequential load-change-save twice succeeds; an entity without `HasOptimisticConcurrency()` keeps last-write-wins; the token value differs before and after an update. Verify green on both providers.
- [ ] 1.6 (test) Soft-delete and tenant interaction: stale soft-delete of a version-tracked entity is rejected; tenant filter still hides another tenant's version-tracked entity. Verify green on both providers.

## 2. Translate the failure out of EF Core

- [ ] 2.1 Add `src/Raftel.Application/Exceptions/ConcurrencyConflictException.cs` (sealed, exposes the conflicting entity type names, keeps the inner exception); verify with a unit test of its message and `EntityNames`.
- [ ] 2.2 (test) In `ConcurrencyTests`, assert the stale save now surfaces `ConcurrencyConflictException` naming the entity type, through both `CommitAsync` and a direct `SaveChangesAsync`, and that a non-concurrency `DbUpdateException` (e.g. unique index violation) propagates unchanged.
- [ ] 2.3 Override `SaveChangesAsync(bool, CancellationToken)` and `SaveChanges(bool)` in `RaftelDbContext` to rethrow `DbUpdateConcurrencyException` as `ConcurrencyConflictException`, and route `CommitAsync` through `this.SaveChangesAsync`; verify 2.2 green on both providers and that `TransactionTests` / `UnitOfWorkTests` still pass.
- [ ] 2.4 Add an architecture test in `tests/Raftel.ArchitectureTests` asserting no type in `Raftel.Application` references `DbUpdateConcurrencyException` (or any `Microsoft.EntityFrameworkCore` type); verify it passes.

## 3. Conflict as a `Result`

- [ ] 3.1 (test) `tests/Raftel.Application.UnitTests/Middlewares/ConcurrencyMiddlewareTests.cs`: `next` throws `ConcurrencyConflictException` ⇒ failed result, `ErrorType.Conflict`, code `Concurrency.Conflict`; success passes through unchanged; a different exception propagates. Same matrix for the typed variant.
- [ ] 3.2 Implement `src/Raftel.Application/Middlewares/ConcurrencyMiddleware.cs` (`<TRequest>` and `<TRequest, TResult>`); verify 3.1 green.
- [ ] 3.3 Register `ConcurrencyMiddleware<>` / `<,>` before `TransactionMiddleware` in `demo/Raftel.Api.FunctionalTests.DemoApi/Program.cs` and in the integration test host; verify the solution builds and existing functional tests pass.
- [ ] 3.4 (test) `tests/Raftel.Application.IntegrationTests/LostUpdateTests.cs`: a command whose handler loads a version-tracked aggregate, lets a second context update it, then saves ⇒ failed result `Concurrency.Conflict`; and a command that writes one aggregate then conflicts on a second persists nothing. Verify green.
- [ ] 3.5 (test) Functional test in `tests/Raftel.Api.FunctionalTests`: a command endpoint that ends in a conflict (no `If-Match`) answers 409 with Problem Details `code = Concurrency.Conflict`. Verify green.
- [ ] 3.6 Document the middleware and its required position (outside `TransactionMiddleware`) in `src/Raftel.Application/CLAUDE.md`; verify the documented order matches the demo registration.

## 4. Audit exclusion

- [ ] 4.1 (test) In `tests/Raftel.Infrastructure.Tests/Data/Auditing`, assert that creating and updating an audited, version-tracked entity yields no `Version` property change, on both providers.
- [ ] 4.2 Add `ShadowPropertyNames.Version` to `ChangeSnapshotExtractor.FrameworkShadowProperties`; verify 4.1 green.

## 5. Version exchange abstractions

- [ ] 5.1 Add `src/Raftel.Application/Abstractions/Concurrency/` with `IVersionedResult`, `IEntityVersionReader`, `IExpectedVersion` (+ its scoped default implementation), and `src/Raftel.Application/ConcurrencyOptions.cs` (`EnableETags = false`) with `RaftelApplicationBuilder.ConfigureConcurrency`; verify with a `DependencyInjectionTests` case that options and `IExpectedVersion` resolve and the default is off.
- [ ] 5.2 (test) Infrastructure test for `IEntityVersionReader`: returns a non-empty string for a tracked version-tracked entity, a different string after an update, and `null` for an untracked or non-version-tracked entity — on both providers.
- [ ] 5.3 Implement the Infrastructure `IEntityVersionReader` (Base64 for `byte[]`, decimal for `uint`) and register it; verify 5.2 green.
- [ ] 5.4 (test) Infrastructure test for expected-version enforcement: with `IExpectedVersion` set to the current version the save succeeds; set to a stale version it throws `ConcurrencyConflictException` even though the entity was freshly loaded; set to an undecodable value it throws `ConcurrencyConflictException`; a second save in the same scope is not affected once the expectation is consumed. Both providers.
- [ ] 5.5 Implement `ExpectedVersionInterceptor` in `src/Raftel.Infrastructure/Data/Interceptors` and register it with the other interceptors in `DependencyInjection.cs`; verify 5.4 green.

## 6. ETag on query endpoints

- [ ] 6.1 Make the `GetTenant` response implement `IVersionedResult` and fill it via `IEntityVersionReader`; add a versioned GET-by-id + PUT pair to the functional-test demo API over a version-tracked demo aggregate. Verify with a unit test that the `GetTenant` handler returns a non-empty version and that the solution builds.
- [ ] 6.2 (test) Functional tests: with ETags enabled a versioned GET returns a quoted `ETag`; the `ETag` differs after a write; a non-versioned GET has no `ETag`; with ETags disabled no `ETag` is sent.
- [ ] 6.3 Emit the header in `QueryEndpointMapper` when `ConcurrencyOptions.EnableETags` is on and the result is an `IVersionedResult` with a non-empty version; verify 6.2 green.

## 7. If-Match on command endpoints

- [ ] 7.1 (test) Functional tests for the full flow: GET → PUT with matching `If-Match` succeeds; two readers, first PUT succeeds, second PUT with its stale `ETag` gets 412 with `code = Concurrency.Conflict` and the first change is still persisted; well-formed but unknown tag ⇒ 412; malformed header ⇒ 400 and no dispatch; `If-Match: *` behaves like no header; no header ⇒ last write wins; ETags disabled ⇒ header ignored.
- [ ] 7.2 Add `src/Raftel.Application/Authorization/RequiresExpectedVersionAttribute.cs` and a demo command carrying it; (test) functional tests: missing `If-Match` ⇒ 428 Problem Details without dispatch, `*` ⇒ 428, ETags disabled ⇒ command runs.
- [ ] 7.3 Implement `If-Match` parsing, `IExpectedVersion.Expect`, the 428 check and the 412 rendering of `Concurrency.Conflict` in `CommandEndpointMapper` (both `MapCommandEndpoint` and `MapCommandEndpointWithResult`, shared helper); verify 7.1 and 7.2 green and that 3.5 still answers 409.
- [ ] 7.4 Document ETag / `If-Match` behaviour, its single-aggregate limitation and `[RequiresExpectedVersion]` in `src/Raftel.Api.Server/CLAUDE.md`; verify the documented status codes match the functional tests.

## 8. Migrations and docs

- [ ] 8.1 Add the EF migration for the demo context (`demo/Raftel.Demo.Infrastructure/Data/Migrations`) covering the new token; verify `dotnet ef migrations has-pending-model-changes` reports none for the demo context.
- [ ] 8.2 Add an "Optimistic concurrency" section to `docs/database-configuration.md`: opt-in call, per-provider mapping, migration impact (SQL Server column, none on PostgreSQL), middleware registration, ETag options; verify it is linked from `docs/toc.yml` if that page's sections are listed there.
- [ ] 8.3 Update `docs/backlog/08-concurrencia-optimista.md`: status, acceptance criteria met, and a note that §2.5 (retry) is deferred and that the design departed from `IHasVersion` / `HasOptimisticConcurrency(provider)`; verify each acceptance criterion maps to a passing test named in the note.

## 9. Integration check

- [ ] 9.1 `dotnet build Raftel.sln` with zero errors and no new warnings.
- [ ] 9.2 `dotnet test` green across all projects, including both Testcontainers providers.
- [ ] 9.3 `openspec validate optimistic-concurrency --strict` passes.
