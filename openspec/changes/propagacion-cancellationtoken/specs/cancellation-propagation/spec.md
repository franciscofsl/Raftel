## Purpose

Ensures a caller's `CancellationToken` flows unbroken from the HTTP endpoint through the command/query dispatch pipeline, every middleware, the handler, and into repository/database calls, so aborted or timed-out requests actually stop doing work instead of running to completion unattended.

## ADDED Requirements

### Requirement: Token flows from endpoint to handler
Every command and query dispatch SHALL accept a `CancellationToken` and propagate the same token, unmodified, to the resolved handler.

#### Scenario: Client aborts an in-flight request
- **WHEN** the HTTP client aborts a request while a query is executing
- **THEN** the `CancellationToken` observed by the query handler and its repository call transitions to `IsCancellationRequested == true`, and the underlying database call is cancelled rather than run to completion

#### Scenario: Dispatch with an already-cancelled token
- **WHEN** a command or query is dispatched with a token that is already cancelled
- **THEN** the handler receives that same cancelled token (`IsCancellationRequested == true`) instead of a fresh, uncancelled one

### Requirement: Middleware pipeline preserves cancellation and ordering
Every middleware in the pipeline SHALL receive the `CancellationToken` and forward it to the next stage, without altering middleware execution order.

#### Scenario: Multiple middlewares registered
- **WHEN** three middlewares are registered for a request and the pipeline executes
- **THEN** they execute in their registered order and each one receives the same `CancellationToken` instance passed to the dispatcher

### Requirement: Commit is not interrupted by cancellation
The unit-of-work commit stage SHALL execute with `CancellationToken.None`, regardless of the cancellation state of the token used for the rest of the request.

#### Scenario: Token cancelled after handler succeeds but before commit
- **WHEN** the caller cancels the request's token after the handler has produced a successful `Result` but before the unit of work commits
- **THEN** the commit still runs to completion (not cancelled) and domain events dispatched as part of that commit are not aborted

### Requirement: Public async operations declare a cancellation token
Every public method whose name ends in `Async` in `Raftel.Application` and `Raftel.Domain` SHALL declare a `CancellationToken` parameter.

#### Scenario: New public async method added without a token
- **WHEN** a new public `*Async` method is added to `Raftel.Application` or `Raftel.Domain` without a `CancellationToken` parameter
- **THEN** the architecture test suite fails, flagging the method as missing cancellation support
