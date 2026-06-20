# Raftel.Infrastructure.Tests

**Infrastructure integration** tests: `RaftelDbContext`, global filters (soft-delete + multitenancy), interceptors (audit, tenant, change tracking), `EfRepository` repositories, and audit logging (`AuditStore`). Run against **real containerized databases**, both SQL Server **and** PostgreSQL.

## Stack and Fixtures

- **xUnit 2.9.3** + **Shouldly** + **NSubstitute**.
- **Testcontainers.MsSql** and **Testcontainers.PostgreSql**.
- Fixtures/bases:
  - `SqlServerTestContainerFixture` / `PostgreSqlTestContainerFixture` — container lifecycle (collection fixtures).
  - `InfrastructureTestBase` (SQL Server) and `PostgreSqlInfrastructureTestBase` (PostgreSQL) — base per test with real `DbContext`.
  - `GlobalUsings.cs` for common usings.

## Conventions

- Inherit from the appropriate base for the provider you want to cover; many behaviors (filters, interceptors) should be verified on **both** providers.
- To test filter disabling use `IDataFilter.Disable<TFilter>()` inside a `using` and verify query behavior change.
- Isolation per test; don't depend on execution order.
- Verify real DB effects (audit columns, `TenantId`, `IsDeleted`), not mocks.
