# Raftel.Infrastructure.Tests

**Infrastructure integration** tests: `RaftelDbContext`, global filters (soft-delete + multitenancy), interceptors (audit, tenant, change tracking), `EfRepository` repositories, and audit logging (`AuditStore`). Run against **real containerized databases**, both SQL Server **and** PostgreSQL.

## Stack and Fixtures

- **xUnit 2.9.3** + **Shouldly** + **NSubstitute** + **Respawn**.
- **Testcontainers.MsSql** and **Testcontainers.PostgreSql**.
- One SQL Server container and one PostgreSQL container for the **whole assembly run**, not per test:
  - `SqlServerTestCollection` / `PostgreSqlTestCollection` — `[CollectionDefinition]` declaring `SqlServerTestContainerFixture` / `PostgreSqlTestContainerFixture` as `ICollectionFixture<T>`. xUnit starts each container once and disposes it once, at assembly scope.
  - `InfrastructureTestBase` (SQL Server) and `PostgreSqlInfrastructureTestBase` (PostgreSQL) — receive the shared fixture via constructor, build a fresh `ServiceProvider`/`DbContext` per test, then reset table data with `Respawner` (so each test still starts from an empty database without restarting the container).
  - `GlobalUsings.cs` for common usings.
- Every concrete test class must declare `[Collection(SqlServerTestCollection.Name)]` (or `PostgreSqlTestCollection.Name`) and a constructor forwarding the injected fixture to `base(fixture)` — this is what makes it share the container with its siblings.

## Conventions

- Inherit from the appropriate base for the provider you want to cover; many behaviors (filters, interceptors) should be verified on **both** providers.
- To test filter disabling use `IDataFilter.Disable<TFilter>()` inside a `using` and verify query behavior change.
- Isolation per test is guaranteed by the Respawn reset in `InitializeAsync`, not by a fresh container — don't add manual container creation in a test.
- Verify real DB effects (audit columns, `TenantId`, `IsDeleted`), not mocks.
