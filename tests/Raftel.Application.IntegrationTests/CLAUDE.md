# Raftel.Application.IntegrationTests

**Integration** tests of server-side use cases end-to-end (handler + EF Core + real database), no HTTP. Verify application pipeline works against real persistence.

## Stack

- **xUnit 2.9.3** + **Shouldly** + **NSubstitute**.
- **Testcontainers** (SQL Server / PostgreSQL) for ephemeral DB per run.
- `IntegrationTestBase.cs` base class that spins up container, builds real `ServiceProvider` (Application + Infrastructure), and exposes dispatchers.

## Conventions

- Inherit from `IntegrationTestBase`; don't manually instantiate containers or `DbContext`.
- Each test must be **isolated**: don't assume other tests' data; clean/seed what you need.
- Test full flow: dispatch a real Command/Query and verify persisted state.
- These tests are slow (spin Docker): keep them focused on integration, leave fine logic to unit tests.
