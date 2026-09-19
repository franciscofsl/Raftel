## 1. Application abstractions

- [ ] 1.1 Add `src/Raftel.Application/ITransaction.cs` — `CommitAsync`, `RollbackAsync`, `IAsyncDisposable`, with XML docs stating that commit/rollback are not cancellable.
- [ ] 1.2 Extend `src/Raftel.Application/IUnitOfWork.cs` with `Task<ITransaction> BeginTransactionAsync(CancellationToken = default)` and `bool HasActiveTransaction`; leave `CommitAsync`'s signature unchanged.
- [ ] 1.3 Add `src/Raftel.Application/TransactionOptions.cs` with `IsolationLevel` defaulting to `IsolationLevel.ReadCommitted`.

## 2. Middleware (test first)

- [ ] 2.1 (test) `tests/Raftel.Application.UnitTests/Middlewares/TransactionMiddlewareTests.cs` with a substituted `IUnitOfWork`: successful `Result` ⇒ `CommitAsync` once, `RollbackAsync` never.
- [ ] 2.2 (test) Failed `Result` ⇒ rollback, no commit, failed result returned unchanged.
- [ ] 2.3 (test) Handler throws ⇒ rollback and the original exception propagates.
- [ ] 2.4 (test) `HasActiveTransaction == true` ⇒ `BeginTransactionAsync` never called, `next` still invoked, no commit or rollback.
- [ ] 2.5 (test) Already-cancelled request token ⇒ commit and rollback still run (assert they receive `CancellationToken.None`).
- [ ] 2.6 (test) Same matrix for the typed variant, asserting the typed value is returned unchanged.
- [ ] 2.7 Implement `src/Raftel.Application/Middlewares/TransactionMiddleware.cs` — both `TransactionMiddleware<TRequest>` and `TransactionMiddleware<TRequest, TResult>` — with `await using`, `next(cancellationToken)`, and commit/rollback on `CancellationToken.None`.

## 3. Infrastructure implementation

- [ ] 3.1 Add `src/Raftel.Infrastructure/Data/RaftelTransaction.cs` wrapping `IDbContextTransaction`: `MarkRollbackOnly()`, commit that throws `InvalidOperationException` after rolling back when rollback-only, and `DisposeAsync` that rolls back if neither commit nor rollback ran.
- [ ] 3.2 Add `src/Raftel.Infrastructure/Data/NestedTransaction.cs` — no-op commit, `RollbackAsync` marking the root rollback-only, no-op dispose.
- [ ] 3.3 Implement `BeginTransactionAsync` and `HasActiveTransaction` in `src/Raftel.Infrastructure/Data/RaftelDbContext.cs`, starting the physical transaction with the configured isolation level and clearing the active-transaction state on disposal.
- [ ] 3.4 Bind `TransactionOptions` in `src/Raftel.Infrastructure/DependencyInjection.cs` and make it available to the `DbContext`.

## 4. Infrastructure tests (Testcontainers)

- [ ] 4.1 `tests/Raftel.Infrastructure.Tests/Data/TransactionTests.cs`: begin/commit persists; begin/rollback does not; `HasActiveTransaction` flips true then false.
- [ ] 4.2 Disposing an uncommitted transaction rolls it back — nothing persisted.
- [ ] 4.3 Nested begin opens no second physical transaction and its commit is a no-op.
- [ ] 4.4 Nested rollback ⇒ root `CommitAsync` throws `InvalidOperationException` and nothing is persisted.
- [ ] 4.5 Configured isolation level is the one the started transaction uses (default `ReadCommitted`).
- [ ] 4.6 Run 4.1–4.5 against both the SQL Server and PostgreSQL fixtures.

## 5. Integration tests

- [ ] 5.1 `tests/Raftel.Application.IntegrationTests/TransactionRollbackTests.cs`: command writing two aggregates and failing on the second ⇒ no rows persisted.
- [ ] 5.2 Domain event handler that writes to the database ⇒ its writes persist when the command succeeds.
- [ ] 5.3 Domain event handler write is reverted when the command's transaction rolls back.
- [ ] 5.4 Audit log rows written during a rolled-back command are not persisted.
- [ ] 5.5 A query handled through the pipeline opens no transaction.

## 6. Wiring and documentation

- [ ] 6.1 Register `TransactionMiddleware<>` / `TransactionMiddleware<,>` before `UnitOfWorkMiddleware` in `demo/Raftel.Api.FunctionalTests.DemoApi/Program.cs`.
- [ ] 6.2 Document the transactional guarantee — and its limit for non-database side effects — in the XML doc of `IDomainEventHandler`.
- [ ] 6.3 Document the required middleware ordering in the Application layer `CLAUDE.md` (and the demo's, if it documents the pipeline).
- [ ] 6.4 Mark `docs/backlog/05-transacciones-explicitas.md` acceptance criteria as met and update its status.

## 7. Verification

- [ ] 7.1 `dotnet build Raftel.sln` clean.
- [ ] 7.2 `dotnet test` green, architecture tests included (no EF Core types leaking into `Raftel.Application`).
