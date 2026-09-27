## Why

`UnitOfWorkMiddleware` commits with `SaveChanges`, not with an explicit transaction. EF Core wraps a single `SaveChanges` in its own implicit transaction, so the trivial case works, but atomicity is lost the moment a request produces more than one `SaveChanges`: a handler that commits mid-way to obtain a generated id and then keeps mutating, a domain event handler that writes to the database (dispatch happens in `SavedChanges`, i.e. after the command's commit), or the audit store persisting separately. There is no rollback at all, so a failure after an intermediate commit leaves the system in an inconsistent state.

## What Changes

- Add `ITransaction` to `Raftel.Application` — an EF-free abstraction with `CommitAsync`, `RollbackAsync` and `IAsyncDisposable`.
- Extend `IUnitOfWork` with `BeginTransactionAsync(CancellationToken)` and `HasActiveTransaction`. Additive: existing `CommitAsync` signature is unchanged.
- Add `TransactionMiddleware<TRequest>` and `TransactionMiddleware<TRequest, TResult>` — command-only middleware sitting outside `UnitOfWorkMiddleware`, which opens a transaction, commits it on a successful `Result` and rolls it back on a failed `Result` or an exception (exception rethrown).
- One physical transaction per request: a nested `BeginTransactionAsync` returns a no-op wrapper whose `RollbackAsync` marks the root transaction rollback-only. A rollback-only root refuses to commit and rolls back instead.
- Commit and rollback always run with `CancellationToken.None` — consistent with the existing decision in the cancellation-token propagation change.
- Queries never open a transaction.
- Add `TransactionOptions` with a configurable `IsolationLevel` (default: the provider's, `ReadCommitted`).
- Document the transactional guarantee offered to `IDomainEventHandler` implementations.

Not breaking: all additions are additive and the middleware is opt-in at registration time, like `UnitOfWorkMiddleware`.

## Capabilities

### New Capabilities
- `explicit-transactions`: request-scoped atomic boundary for commands — transaction lifecycle on `IUnitOfWork`, nesting rules, middleware behaviour on success/failure/exception, isolation level configuration, and the guarantee extended to domain event handlers.

### Modified Capabilities
<!-- None: openspec/specs/ has no existing capability whose requirements change. -->

## Impact

- `src/Raftel.Application`: new `ITransaction.cs`, `TransactionOptions.cs`, `Middlewares/TransactionMiddleware.cs`; modified `IUnitOfWork.cs`; XML doc on `IDomainEventHandler`.
- `src/Raftel.Infrastructure`: new `Data/RaftelTransaction.cs`, `Data/NestedTransaction.cs`; `Data/RaftelDbContext.cs` implements the new `IUnitOfWork` members; `DependencyInjection.cs` binds `TransactionOptions`.
- `demo/Raftel.Api.FunctionalTests.DemoApi/Program.cs`: register the middleware ahead of `UnitOfWorkMiddleware`.
- Behaviour change for existing apps that register the new middleware: domain event handler writes now participate in the command transaction. External side effects (email, HTTP) remain unprotected — that is the outbox change (backlog 07).
- Tests: unit (`Raftel.Application.UnitTests`), infrastructure (`Raftel.Infrastructure.Tests`, Testcontainers SQL Server + PostgreSQL) and integration (`Raftel.Application.IntegrationTests`).
- Tracks GitHub issue #129 / `docs/backlog/05-transacciones-explicitas.md`.
