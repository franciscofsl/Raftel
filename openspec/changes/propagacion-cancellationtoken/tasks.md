## 1. Failing tests first

- [x] 1.1 `RequestDispatcherTests`: dispatching with an already-cancelled token results in the handler observing `token.IsCancellationRequested == true`
- [x] 1.2 Middleware ordering test: register 3 middlewares that append to a shared list, assert execution order is preserved after the signature change

## 2. Core abstractions

- [x] 2.1 Add `CancellationToken` parameter to `RequestHandlerDelegate<TResponse>`
- [x] 2.2 Add `CancellationToken` parameter to `IGlobalMiddleware.HandleAsync` (propagates to `ICommandMiddleware<T>`, `ICommandMiddleware<T,R>`, `IQueryMiddleware<T,R>` via inheritance)
- [x] 2.3 Add `CancellationToken cancellationToken = default` to `IRequestDispatcher.DispatchAsync`
- [x] 2.4 Add `CancellationToken cancellationToken = default` to `IQueryDispatcher.DispatchAsync`
- [x] 2.5 Stop `ICommandDispatcher.DispatchAsync` from discarding its existing `CancellationToken` parameter

## 3. Dispatcher implementations

- [x] 3.1 Update `RequestDispatcher.DispatchAsync` to build the pipeline with `token => ...` closures per stage (see design.md - Decisions) instead of capturing the token once
- [x] 3.2 Update `CommandDispatcher` to pass the token through to `RequestDispatcher`
- [x] 3.3 Update `QueryDispatcher` to pass the token through to `RequestDispatcher`

## 4. Framework middlewares

- [x] 4.1 Update `ValidationMiddleware` to the new `HandleAsync` signature
- [x] 4.2 Update `UnitOfWorkMiddleware` to the new signature; commit with `CancellationToken.None` and document why in its XML doc (design.md - Decisions)
- [x] 4.3 Update `PermissionAuthorizationMiddleware` to the new signature
- [x] 4.4 Update `AuditLogMiddleware` to the new signature

## 5. Endpoint mappers

- [x] 5.1 `CommandEndpointMapper`: inject `CancellationToken` in the generated handler and pass it to `dispatcher.DispatchAsync`
- [x] 5.2 `QueryEndpointMapper`: inject `CancellationToken` in the generated handler and pass it to `dispatcher.DispatchAsync`

## 6. Handler sweep

- [x] 6.1 Sweep `src/Raftel.Application/Features/**`: pass the token to every repository call in each handler (compiler errors from step 2 guide this) — already correct in every handler, verified by inspection
- [x] 6.2 Sweep `demo/Raftel.Demo.Application/**` the same way — already correct in every handler, verified by inspection
- [x] 6.3 Verify `IDomainEventsDispatcher` already propagates the token correctly end to end — confirmed; commit path intentionally uses `CancellationToken.None` per §2.5/design

## 7. Tests and demo/test fixture updates

- [x] 7.1 Update `tests/**` call sites broken by the signature changes
- [x] 7.2 Update `demo/**` call sites broken by the signature changes
- [x] 7.3 Integration test: cancelling the token during a long-running query raises `OperationCanceledException` from EF Core
- [x] 7.4 Functional test: client-aborted request does not let the operation complete (test handler awaits on the token)
- [x] 7.5 Unit test: `UnitOfWorkMiddleware` still commits successfully when the caller's token is cancelled after the handler returns a successful `Result`

## 8. Architecture regression guard

- [x] 8.1 Add architecture test: every public `*Async` method in `Raftel.Application` and `Raftel.Domain` declares a `CancellationToken` parameter
- [x] 8.2 Run the new test against the current codebase and fix any remaining gaps it finds — passed immediately, no gaps found

## 9. Documentation and acceptance

- [x] 9.1 Update `BREAKING_CHANGES.md` with the new signatures and a migration snippet for third-party middleware
- [x] 9.2 Run `dotnet test` for the full solution and confirm green — 361/361 passing
- [x] 9.3 Verify acceptance criteria from proposal.md / spec are all met
