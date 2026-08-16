## Why

`CancellationToken` exists at both ends of the request pipeline (minimal API endpoint, `IRequestHandler`, `IRepository`) but gets **dropped in the middle**: `ICommandDispatcher.DispatchAsync` discards it, `IRequestDispatcher` and `IQueryDispatcher` don't declare it, `RequestHandlerDelegate` and `IGlobalMiddleware` don't accept it. As a result no operation is actually cancelable — an aborted HTTP request keeps hitting the database until it finishes, wasting connections under load on slow endpoints.

## What Changes

- **BREAKING**: `RequestHandlerDelegate<TResponse>` gains a `CancellationToken` parameter.
- **BREAKING**: `IGlobalMiddleware.HandleAsync` (and by inheritance `ICommandMiddleware<T>`, `ICommandMiddleware<T,R>`, `IQueryMiddleware<T,R>`) gains a third `CancellationToken` parameter.
- **BREAKING**: `IRequestDispatcher.DispatchAsync` and `IQueryDispatcher.DispatchAsync` add a `CancellationToken cancellationToken = default` parameter; `ICommandDispatcher.DispatchAsync` stops discarding the token it already accepts.
- `RequestDispatcher` rebuilds the middleware pipeline to thread the token through every stage instead of closing over it once.
- Minimal-API endpoint mappers (`CommandEndpointMapper`, `QueryEndpointMapper`) inject `CancellationToken` (bound to `HttpContext.RequestAborted`) and pass it to the dispatcher call.
- Framework middlewares (`Validation`, `UnitOfWork`, `PermissionAuthorization`, `AuditLog`) updated to the new signature. `UnitOfWorkMiddleware` deliberately commits with `CancellationToken.None` — cancelling mid-`SaveChanges` leaves the transaction/domain-event dispatch in an indeterminate state — and this exception is documented in its XML doc.
- All handlers under `src/Raftel.Application/Features/**` and `demo/Raftel.Demo.Application/**` pass the token through to repository calls instead of calling them with an implicit default.
- New architecture test asserting every public `*Async` method in `Raftel.Application` and `Raftel.Domain` declares a `CancellationToken` parameter, as a permanent regression guard.
- `BREAKING_CHANGES.md` updated with migration guidance for third-party middlewares.

## Capabilities

### New Capabilities
- `cancellation-propagation`: end-to-end `CancellationToken` propagation through the command/query pipeline — dispatchers, middleware chain, handlers, repositories — and the commit-time exception for `UnitOfWorkMiddleware`.

### Modified Capabilities
(none — no existing `openspec/specs/` capabilities yet; this is a net-new capability)

## Impact

- **Affected code**: `Raftel.Application` abstractions (`RequestHandlerDelegate`, `IRequestDispatcher`/`RequestDispatcher`, `ICommandDispatcher`/`CommandDispatcher`, `IQueryDispatcher`/`QueryDispatcher`), all `IGlobalMiddleware`/`ICommandMiddleware`/`IQueryMiddleware` implementations, every handler under `Features/**`; `Raftel.Api.Server` endpoint mappers; `demo/**` and `tests/**`.
- **Public API**: breaking signature changes to `IGlobalMiddleware`, `RequestHandlerDelegate`, `IRequestDispatcher`, `IQueryDispatcher` — any third-party middleware or dispatcher implementation must update to compile.
- **Dependencies**: none new.
- **Tests**: new architecture test, unit tests for pipeline propagation and middleware ordering, integration test for query cancellation via EF Core.
