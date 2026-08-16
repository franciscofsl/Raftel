## 1. Failing tests first

- [ ] 1.1 `RequestDispatcherTests`: dispatching with an already-cancelled token results in the handler observing `token.IsCancellationRequested == true`
- [ ] 1.2 Middleware ordering test: register 3 middlewares that append to a shared list, assert execution order is preserved after the signature change

## 2. Core abstractions

- [ ] 2.1 Add `CancellationToken` parameter to `RequestHandlerDelegate<TResponse>`
- [ ] 2.2 Add `CancellationToken` parameter to `IGlobalMiddleware.HandleAsync` (propagates to `ICommandMiddleware<T>`, `ICommandMiddleware<T,R>`, `IQueryMiddleware<T,R>` via inheritance)
- [ ] 2.3 Add `CancellationToken cancellationToken = default` to `IRequestDispatcher.DispatchAsync`
- [ ] 2.4 Add `CancellationToken cancellationToken = default` to `IQueryDispatcher.DispatchAsync`
- [ ] 2.5 Stop `ICommandDispatcher.DispatchAsync` from discarding its existing `CancellationToken` parameter

## 3. Dispatcher implementations

- [ ] 3.1 Update `RequestDispatcher.DispatchAsync` to build the pipeline with `token => ...` closures per stage (see design.md - Decisions) instead of capturing the token once
- [ ] 3.2 Update `CommandDispatcher` to pass the token through to `RequestDispatcher`
- [ ] 3.3 Update `QueryDispatcher` to pass the token through to `RequestDispatcher`

## 4. Framework middlewares

- [ ] 4.1 Update `ValidationMiddleware` to the new `HandleAsync` signature
- [ ] 4.2 Update `UnitOfWorkMiddleware` to the new signature; commit with `CancellationToken.None` and document why in its XML doc (design.md - Decisions)
- [ ] 4.3 Update `PermissionAuthorizationMiddleware` to the new signature
- [ ] 4.4 Update `AuditLogMiddleware` to the new signature

## 5. Endpoint mappers

- [ ] 5.1 `CommandEndpointMapper`: inject `CancellationToken` in the generated handler and pass it to `dispatcher.DispatchAsync`
- [ ] 5.2 `QueryEndpointMapper`: inject `CancellationToken` in the generated handler and pass it to `dispatcher.DispatchAsync`

## 6. Handler sweep

- [ ] 6.1 Sweep `src/Raftel.Application/Features/**`: pass the token to every repository call in each handler (compiler errors from step 2 guide this)
- [ ] 6.2 Sweep `demo/Raftel.Demo.Application/**` the same way
- [ ] 6.3 Verify `IDomainEventsDispatcher` already propagates the token correctly end to end

## 7. Tests and demo/test fixture updates

- [ ] 7.1 Update `tests/**` call sites broken by the signature changes
- [ ] 7.2 Update `demo/**` call sites broken by the signature changes
- [ ] 7.3 Integration test: cancelling the token during a long-running query raises `OperationCanceledException` from EF Core
- [ ] 7.4 Functional test: client-aborted request does not let the operation complete (test handler awaits on the token)
- [ ] 7.5 Unit test: `UnitOfWorkMiddleware` still commits successfully when the caller's token is cancelled after the handler returns a successful `Result`

## 8. Architecture regression guard

- [ ] 8.1 Add architecture test: every public `*Async` method in `Raftel.Application` and `Raftel.Domain` declares a `CancellationToken` parameter
- [ ] 8.2 Run the new test against the current codebase and fix any remaining gaps it finds

## 9. Documentation and acceptance

- [ ] 9.1 Update `BREAKING_CHANGES.md` with the new signatures and a migration snippet for third-party middleware
- [ ] 9.2 Run `dotnet test` for the full solution and confirm green
- [ ] 9.3 Verify acceptance criteria from proposal.md / spec are all met
