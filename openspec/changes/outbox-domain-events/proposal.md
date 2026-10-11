# Proposal

## Why

Domain events are dispatched in-process by `DomainEventsDispatchInterceptor`, synchronously, right after `SaveChanges`. A handler that throws makes a command fail even though its state change already persisted, the event is gone for good (no retry, no record), a process crash between the write and the dispatch loses it silently, and every handler's latency is added to the HTTP response. `TransactionMiddleware` (backlog 05) made handler *database* writes atomic with the command, but external side effects — email, HTTP calls — remain unprotected. The transactional outbox is the fix `docs/domain-events-plan.md` already names as the next step, and four backlog items (#135, #138, #140, #141) are blocked on it.

## What Changes

- Add an opt-in **transactional outbox**: when enabled, domain events raised by tracked aggregates are stored as outbox messages in the same `SaveChanges` (and therefore the same transaction) as the state change, instead of being dispatched in-process.
- Add a background **outbox processor** that claims pending messages in batches, restores the originating tenant and correlation id, and dispatches each event to its `IDomainEventHandler<T>` implementations.
- **Retries with exponential backoff** (capped) for failed messages, and **dead-lettering** once the configured attempt limit is exhausted. Dead letters stay stored and queryable.
- **Multi-instance safety**: a message is leased to one processor at a time, with provider-specific claiming for SQL Server and PostgreSQL.
- **At-least-once delivery, made idempotent per handler**: the framework records a receipt per (message, handler) in the same transaction as that handler's database writes, so a redelivered message does not re-run a handler that already completed.
- **Type allow-list** on deserialization: only event types with a registered handler are ever stored or materialised; stored type names are never resolved through arbitrary type loading.
- **Retention cleanup**: a background service deletes processed messages (and their receipts) older than a configurable retention period. Dead letters are never auto-deleted.
- Add `OutboxOptions` (`Enabled`, `BatchSize`, `PollingInterval`, `MaxAttempts`, `BaseRetryDelay`, `MaxRetryDelay`, `LockDuration`, `RetentionPeriod`), configured through a new optional `configureOutbox` callback on `AddRaftelData`.
- Add `ICorrelationScope` so non-HTTP work can run under an explicit correlation id; `ICorrelationContext` itself stays read-only.
- `Enabled = false` (the default) keeps today's in-process dispatch byte-for-byte: same interceptor, same timing, no background services.

No public API is broken. One schema consequence applies to every consumer, enabled or not: the framework model gains two tables (outbox messages, handler receipts), so applications that manage their schema with EF migrations need a new migration after upgrading.

## Capabilities

### New Capabilities
- `domain-events-outbox`: durable, transactional capture of domain events and their asynchronous delivery — capture atomicity, decoupling handler failures from commands, tenant/correlation restoration, retry and dead-letter policy, single-claim across processor instances, per-handler idempotency, type allow-listing, retention, and the disabled-mode compatibility guarantee.

### Modified Capabilities
<!-- None: openspec/specs/ does not exist yet, so there is no archived capability whose requirements change. The handler transaction guarantee from the in-flight `explicit-transactions` change is unchanged when the outbox is disabled. -->

## Impact

- `src/Raftel.Domain`: new `Outbox/OutboxMessage.cs`, `Outbox/ValueObjects/OutboxMessageId.cs`.
- `src/Raftel.Application`: new `OutboxOptions.cs`; new `Abstractions/Outbox/` (`IOutboxStore`, `IOutboxSerializer`, `IIdempotencyStore`, `IDomainEventTypeRegistry`); new `Abstractions/ICorrelationScope.cs`; `DependencyInjection.cs` builds the event type registry from the scanned handlers; XML doc on `IDomainEventHandler` updated for the outbox mode.
- `src/Raftel.Infrastructure`: new `Outbox/` (processor, per-provider store, serializer, idempotency store, cleanup service, message dispatcher), `Data/Interceptors/OutboxWriteInterceptor.cs`, `Data/Configuration/Outbox/`; modified `Data/RaftelDbContext.cs` (`DbSet<OutboxMessage>`, provider-specific filtered index), `Correlation/CorrelationContext.cs`, `Data/Auditing/ChangeSnapshotExtractor.cs` (exclude outbox entities from auditing), `DependencyInjection.cs`.
- Dependencies: `Microsoft.Extensions.Hosting.Abstractions` (for `BackgroundService`) if not already reachable from `Raftel.Infrastructure`. No third-party packages.
- Database: two new tables plus a filtered index on pending messages; consumers on EF migrations must add a migration. The demo app's migrations are regenerated accordingly.
- Behaviour change **only when enabled**: handlers run after the command has returned, in a separate DI scope with no `HttpContext`; a handler failure no longer surfaces to the caller; events with no registered handler are not stored.
- Tests: `Raftel.Domain.Tests`, `Raftel.Application.UnitTests`, `Raftel.Infrastructure.Tests` (Testcontainers, SQL Server + PostgreSQL), `Raftel.Application.IntegrationTests`.
- Tracks GitHub issue #131 / `docs/backlog/07-outbox-domain-events.md`. Depends on #128 (structured logging) and #129 (explicit transactions), both already in this branch. Unblocks #135, #138, #140, #141.
