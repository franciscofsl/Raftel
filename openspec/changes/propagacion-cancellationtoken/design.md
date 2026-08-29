## Context

See proposal.md - Why. The pipeline today is: `HTTP endpoint → IRequestDispatcher/ICommandDispatcher/IQueryDispatcher → IGlobalMiddleware chain → IRequestHandler → IRepository → EF Core`. `IRepository` already accepts a `CancellationToken` on every method; the gap is everything above it not carrying the token down.

## Goals / Non-Goals

**Goals:**
- A single `CancellationToken`, originating at `HttpContext.RequestAborted`, reaches every stage of the pipeline unchanged.
- Third-party middleware and dispatcher implementations have one clear, documented signature to migrate to.
- Commit-time work is protected from mid-transaction cancellation.

**Non-Goals:**
- Making the commit itself cancellable (explicitly rejected, see Decisions).
- Introducing per-request timeouts or deadline propagation beyond what `HttpContext.RequestAborted` already provides.
- Retrofitting cancellation into fire-and-forget/background work (out of scope; tracked separately if needed).

## Decisions

### Token flows as an explicit parameter, not via the request object
Add `CancellationToken` as an explicit parameter on `RequestHandlerDelegate<TResponse>`, `IGlobalMiddleware.HandleAsync`, `IRequestDispatcher.DispatchAsync`, and `IQueryDispatcher.DispatchAsync` (mirroring what `ICommandDispatcher` already declares but currently discards).

**Alternatives considered:**
- *Token embedded in the command/query object*: rejected — commands/queries are DTOs deserialized from JSON; embedding infrastructure concerns in them breaks that contract.
- *`AsyncLocal<CancellationToken>` ambient context*: rejected — implicit, hard to unit test, and unreliable across thread-pool continuations.
- *New overloads alongside the old ones*: rejected — doubles the pipeline surface and leaves a permanent path with no live cancellation, defeating the purpose.

### Pipeline is rebuilt per-dispatch with the token closed over per stage
`RequestDispatcher.DispatchAsync` builds the middleware chain via `Aggregate`, wrapping the terminal handler delegate and composing each middleware around it. The token is passed as the argument to the resulting `RequestHandlerDelegate<TResponse>` at invocation time, not captured once at construction:

```csharp
var handlerDelegate = new RequestHandlerDelegate<TResponse>(
    token => handler.HandleAsync(request, token));

var pipeline = allMiddlewares
    .Reverse<IGlobalMiddleware<TRequest, TResponse>>()
    .Aggregate(handlerDelegate,
        (next, middleware) => token => middleware.HandleAsync(request, next, token));

return await pipeline(cancellationToken);
```
`next` is captured by value per `Aggregate` iteration, which C#'s closure semantics already guarantee; a dedicated ordering test (3 middlewares writing to a shared list) verifies this holds after the signature change rather than relying on reasoning alone.

### Commit runs with `CancellationToken.None`
`UnitOfWorkMiddleware` deliberately does **not** propagate the caller's token into `SaveChanges`. Cancelling mid-commit leaves the transaction in an indeterminate state and can abort in-flight domain event dispatch. This is documented in the middleware's XML doc as a permanent, intentional exception to "propagate everywhere."

**Alternative considered:** propagate the token to commit and rely on the DB provider to roll back cleanly on cancellation — rejected as provider-dependent and unsafe once domain events have started dispatching.

### Architecture test as the regression guard
A reflection-based test asserts every public `*Async` method in `Raftel.Application` and `Raftel.Domain` declares a `CancellationToken` parameter. This is cheaper and more durable than relying on code review to catch a dropped token in a new handler or dispatcher method added later.

## Risks / Trade-offs

- **Breaking change for third-party middleware/dispatchers** → Mitigated by documenting the exact new signatures in `BREAKING_CHANGES.md` with a before/after snippet; the compiler catches every call site inside the framework itself.
- **Large mechanical diff across `Features/**` handlers** → Mitigated by doing the signature changes first (steps 3-5 in tasks.md), which breaks the build and turns the remaining work into a compiler-guided checklist rather than a manual audit.
- **Risk of silently swallowing `OperationCanceledException` somewhere in the pipeline** → Mitigated by the functional test (aborted client request) and the integration test asserting EF Core raises `OperationCanceledException` for a cancelled long-running query.

## Migration Plan

1. Land framework-level signature changes and the four core middlewares (breaks the build intentionally).
2. Update the three dispatchers.
3. Update endpoint mappers to source the token from `HttpContext.RequestAborted`.
4. Sweep `Features/**` and `demo/**` handlers to pass the token to repository calls, following compiler errors.
5. Add the architecture test last, once the sweep is complete, so it starts green and stays green.
6. No runtime/data migration needed — this is a compile-time API change only. Rollback is a straight revert since no persisted state or schema is touched.
