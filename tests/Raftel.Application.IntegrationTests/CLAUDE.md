# Raftel.Application.IntegrationTests

**Integration** tests of server-side use cases end-to-end (handler + EF Core + real database), no HTTP. Verify application pipeline works against real persistence.

## Stack

- **xUnit 2.9.3** + **Shouldly** + **NSubstitute** + **Respawn**.
- **Testcontainers** (SQL Server), reused from `Raftel.Infrastructure.Tests` via project reference. One container for the whole assembly run, declared by `IntegrationSqlServerTestCollection` (`[CollectionDefinition]` + `ICollectionFixture<SqlServerTestContainerFixture>`).
- `IntegrationTestBase.cs` receives the shared fixture by constructor, builds a real `ServiceProvider` (Application + Infrastructure) per test, and resets table data with `Respawner` between tests instead of restarting the container.

## Conventions

- Inherit from `IntegrationTestBase`; don't manually instantiate containers or `DbContext`.
- Every concrete test class needs `[Collection(IntegrationSqlServerTestCollection.Name)]` and a constructor that takes `SqlServerTestContainerFixture` and forwards it to `base(fixture)`.
- Each test must be **isolated**: don't assume other tests' data; clean/seed what you need. Isolation comes from the Respawn reset, not from a fresh container.
- Test full flow: dispatch a real Command/Query and verify persisted state.
- These tests are slow (spin Docker): keep them focused on integration, leave fine logic to unit tests.
