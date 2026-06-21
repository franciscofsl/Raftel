# Raftel.Infrastructure.Tests

**Infrastructure integration** tests: `RaftelDbContext`, global filters (soft-delete + multitenancy), interceptors (audit, tenant, change tracking), `EfRepository` repositories, and audit logging (`AuditStore`). Run against **real containerized databases**, both SQL Server **and** PostgreSQL.

## Stack and Fixtures

- **xUnit 2.9.3** + **Shouldly** + **NSubstitute** + **Respawn**.
- **Testcontainers.MsSql** and **Testcontainers.PostgreSql**.
- One SQL Server container and one PostgreSQL container for the **whole assembly run**, not per test:
  - `SqlServerTestCollection` / `PostgreSqlTestCollection` — `[CollectionDefinition]` declaring `SqlServerTestContainerFixture` / `PostgreSqlTestContainerFixture` as `ICollectionFixture<T>`. xUnit starts each container once and disposes it once, at assembly scope.
  - `IDbContainerFixture` — abstraction implemented by both fixtures (`ConnectionString`, `Provider`, `RespawnAdapter`). `InfrastructureTestBase` depends on this interface, not on a concrete provider, so the same base works for SQL Server and PostgreSQL.
  - `InfrastructureTestBase` — receives the shared fixture via constructor, builds a fresh `ServiceProvider`/`DbContext` per test, then resets table data with `Respawner` (so each test still starts from an empty database without restarting the container). The Respawner instance is cached **per provider** (not per test class), since both providers' containers run concurrently.
  - `GlobalUsings.cs` for common usings.

### Template Method pattern: one test body, two providers

Test bodies are **never duplicated per provider**. Each behavior is written once in an
`abstract class XxxTestsBase : InfrastructureTestBase`, and two trivial `sealed` classes
derive from it — one per provider, each only adding `[Collection]` + constructor wiring:

```csharp
public abstract class EfRepositoryTestsBase : InfrastructureTestBase
{
    protected EfRepositoryTestsBase(IDbContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetByIdAsync_ShouldRetrieveExpectedData() { /* body written once */ }
}

[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerEfRepositoryTests : EfRepositoryTestsBase
{
    public SqlServerEfRepositoryTests(SqlServerTestContainerFixture fixture) : base(fixture) { }
}

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlEfRepositoryTests : EfRepositoryTestsBase
{
    public PostgreSqlEfRepositoryTests(PostgreSqlTestContainerFixture fixture) : base(fixture) { }
}
```

xUnit discovers and runs every `[Fact]` declared on the base class once per derived
(non-abstract) class, so each behavior is exercised against **both** providers without
copy-pasting the test body.

## Conventions

- New DB-bound behavior: add the test to the relevant `XxxTestsBase`, never directly to a
  `SqlServerXxx`/`PostgreSqlXxx` class — those derived classes must stay wiring-only (constructor
  + `[Collection]`), no test logic and no overrides beyond what the behavior needs from both
  providers (e.g. `ConfigureServices`).
- If a test only makes sense for one provider, add it as a `[Fact]` directly on that provider's
  derived class instead of the shared base.
- To test filter disabling use `IDataFilter.Disable<TFilter>()` inside a `using` and verify query behavior change.
- Isolation per test is guaranteed by the Respawn reset in `InitializeAsync`, not by a fresh container — don't add manual container creation in a test.
- Verify real DB effects (audit columns, `TenantId`, `IsDeleted`), not mocks.
