## Context

See [proposal.md](proposal.md) — Why. Current state that shapes the approach:

- `IUnitOfWork` (`src/Raftel.Application/IUnitOfWork.cs`) has a single member, `Task CommitAsync(CancellationToken)`. `RaftelDbContext<TDbContext>` implements it as `SaveChangesAsync`.
- `UnitOfWorkMiddleware<TRequest>` / `<TRequest, TResult>` already commit with `CancellationToken.None` and only on `IsSuccess`, so the "commit is not cancellable" decision from the cancellation-token change is already established.
- Middlewares are opt-in: the demo registers them explicitly (`cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>))`). Registration order defines pipeline order.
- `Raftel.Application` must not reference `Microsoft.EntityFrameworkCore`; the transaction abstraction therefore lives in Application and its EF implementation in Infrastructure.
- Integration and infrastructure tests run against real SQL Server and PostgreSQL via Testcontainers, so transaction behaviour can be asserted for real; there is no InMemory-provider limitation to work around.

## Goals / Non-Goals

**Goals:**

- One atomic boundary per command, enforced by the pipeline rather than by handler discipline.
- Keep EF Core types out of `Raftel.Application`.
- Additive surface: existing apps that do not register the new middleware behave exactly as today.

**Non-Goals:**

- Protecting non-database side effects (email, outbound HTTP) — that is the outbox change (backlog 07).
- Distributed transactions or multiple connections/contexts in one boundary.
- Transactional reads for queries.
- Retry / resiliency on transient failures (no execution-strategy integration in this change).

## Decisions

### Separate `TransactionMiddleware`, outside `UnitOfWorkMiddleware`

Pipeline order:

```
LoggingMiddleware
  └ ValidationMiddleware
      └ PermissionAuthorizationMiddleware
          └ TransactionMiddleware      ← begin / commit / rollback
              └ UnitOfWorkMiddleware   ← SaveChanges
                  └ Handler
```

Alternative considered: put `BeginTransaction` inside `UnitOfWorkMiddleware`. Rejected — it merges two responsibilities (persisting vs. delimiting the atomic unit) and makes it impossible to use the unit of work without a transaction. Keeping them separate also means the transaction wraps auditing and domain event dispatch, both of which ride on `SaveChanges`.

Alternative considered: `TransactionScope` / `System.Transactions`. Rejected — escalates to a distributed transaction in multi-connection scenarios, weak cross-platform support, and far more machinery than needed.

Alternative considered: open a transaction for the whole scoped `DbContext` at request start. Rejected — holds database resources for the entire request, queries included.

### `ITransaction` in Application, EF wrapper in Infrastructure

```csharp
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

`RaftelTransaction` (Infrastructure) wraps `IDbContextTransaction`; `NestedTransaction` is a no-op wrapper over the root. This keeps the layer dependency rule intact and lets the application layer and its unit tests work against a substituted `IUnitOfWork`.

### `IUnitOfWork` extended additively

```csharp
Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
bool HasActiveTransaction { get; }
```

`CommitAsync` keeps its current `Task` return type. Backlog 05 sketches `Task<int>`; changing it would be a source-breaking change for every existing implementor, and the proposal commits to no public break. If the affected-row count is ever needed it can be added separately.

### Nesting: one physical transaction, rollback-only poisoning

`BeginTransactionAsync` when `HasActiveTransaction` is true returns `NestedTransaction(root)`: `CommitAsync` is a no-op, `RollbackAsync` calls `root.MarkRollbackOnly()`, `DisposeAsync` is a no-op. A root marked rollback-only throws `InvalidOperationException` from `CommitAsync` after performing the rollback — the inner failure is never silently swallowed by an outer commit.

`TransactionMiddleware` itself short-circuits: if `HasActiveTransaction` is true it calls `next` directly and neither commits nor rolls back. The nested-handle path exists for handlers or infrastructure that call `BeginTransactionAsync` on their own.

### Commit/rollback use `CancellationToken.None`

Matches `UnitOfWorkMiddleware`'s existing behaviour and the cancellation-token change's decision. Cancelling mid-commit leaves the transaction indeterminate and can abort in-flight domain event dispatch. `next(cancellationToken)` still receives the real token.

### Disposal is the safety net

`await using var transaction = ...` means an unhandled path (or a rollback that itself fails) still disposes the handle. `RaftelTransaction.DisposeAsync` rolls back if neither commit nor rollback has run, then disposes the underlying `IDbContextTransaction` and clears the context's active-transaction state so `HasActiveTransaction` returns false.

### Commands only

No query middleware. Opening a transaction per query is pure cost; a query needing a consistent multi-statement read opts in explicitly via `IUnitOfWork.BeginTransactionAsync`.

### `TransactionOptions`

```csharp
public sealed class TransactionOptions
{
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}
```

Lives in `Raftel.Application` (`System.Data.IsolationLevel` is BCL, not EF). Bound in `Raftel.Infrastructure/DependencyInjection.cs` and consumed by `RaftelDbContext` when starting the physical transaction, so the default matches both SQL Server's and PostgreSQL's own default and nothing changes for callers that do not configure it.

### Opt-in registration

The middleware is registered by the host app the same way `UnitOfWorkMiddleware` is — registered before it, so it sits outside. The demo `Program.cs` is updated and the ordering requirement is documented alongside it.

## Risks / Trade-offs

- **Longer-held database locks**: the transaction now spans the whole command instead of a single `SaveChanges`. → Commands are short-lived; isolation level stays read-committed by default; queries are excluded entirely.
- **Behaviour change for adopters**: domain event handler writes that previously landed in their own implicit transaction now roll back with the command. → This is the intended fix; called out in the proposal and documented on `IDomainEventHandler`.
- **Rollback-only `InvalidOperationException` surfaces as an exception, not a `Result`**: it signals a programming error (a nested rollback followed by an outer commit), not a business failure. → Rollback still happens before the throw, so no partial state; the error middleware maps it as an unhandled exception.
- **No execution-strategy integration**: EF Core's retrying execution strategies conflict with user-initiated transactions and throw when both are used. → Retrying strategies are not enabled in the current configuration; if one is added later it must wrap the transaction, which is a follow-up change.
- **A handler calling `CommitAsync` mid-way still works** but is no longer two independent transactions — intended, and worth documenting so that callers do not assume intermediate durability.
