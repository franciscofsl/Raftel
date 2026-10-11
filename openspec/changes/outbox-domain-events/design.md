# Design

## Context

See `proposal.md` — Why. Constraints from the current code that shape the approach:

- Dispatch lives in `DomainEventsDispatchInterceptor` (`SavedChanges`), resolved per scope and attached in `AddDataAccess`. `DomainEventsDispatcher` resolves `IDomainEventHandler<T>` by reflection; events without a handler are a no-op.
- `AuditPropertiesInterceptor`/`EntityChangesTrackerInterceptor` already add entities during `SavingChanges`, so adding outbox rows there is an established pattern.
- `IUnitOfWork.BeginTransactionAsync` + `TransactionMiddleware` exist (backlog 05). One physical transaction per unit of work; nested begins are no-ops.
- `CurrentTenant` is `AsyncLocal`-backed with `Change(tenantId)`. `CorrelationContext` is scoped and reads only `HttpContext`; outside a request it invents a new GUID.
- The tenant convention is name-based: any mapped property called `TenantId` of type `Guid?` gets the global tenant filter and is touched by `TenantInterceptor`.
- Options are plain singleton instances configured through `Action<T>` callbacks on `AddRaftelData` (`AuditOptions`, `TransactionOptions`), not `IOptions<T>`.
- Layer rule: no EF in Domain; Application depends only on Domain. Two providers: SQL Server and PostgreSQL.
- Existing events declare `public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;` — a get-only auto-property that `System.Text.Json` does not populate on deserialization.

## Goals / Non-Goals

**Goals:**

- Durable capture in the command's own `SaveChanges`; delivery fully decoupled from the request.
- Correct behaviour with N processor instances and with a processor that crashes at any point.
- Zero behavioural change while `OutboxOptions.Enabled == false`.
- Deterministic tests: every time-dependent rule is driven by `TimeProvider`, and a processing cycle can be run on demand without the hosted-service timer.

**Non-Goals:**

- Strict ordering. Messages are claimed oldest-first, but retries and multiple instances can reorder delivery. Handlers must not assume order.
- Publishing to an external broker, integration events, or cross-service contracts (backlog items this unblocks).
- Lease renewal for long-running handlers, a dead-letter requeue API, an admin endpoint, or an outbox health check.
- Per-event-type opt-in/out. The switch is global.

## Decisions

### D1. Two interceptors, selected at registration

`AddDataAccess` attaches either `DomainEventsDispatchInterceptor` (unchanged) or the new `OutboxWriteInterceptor`, depending on `OutboxOptions.Enabled`. Hosted services are registered only when enabled.

*Why over a runtime branch inside one interceptor:* the disabled path stays literally the same class and code as today, which is the strongest form of the compatibility guarantee, and `DomainEventsDispatchInterceptorTests` keeps passing untouched. The backlog sketch ("conditional on `!Outbox.Enabled`") would put both behaviours in one type.

### D2. Capture in `SavingChanges`; drop events nobody handles

`OutboxWriteInterceptor` (sync and async overrides) walks `ChangeTracker.Entries<IHasDomainEvents>()`, drains each aggregate's events, and for every event whose type has a registered handler adds an `OutboxMessage`. Events with no handler are discarded, matching today's "no handler = no-op". A serialization failure throws, failing the save.

`OutboxMessage` (in `Raftel.Domain/Outbox`, `Entity<OutboxMessageId>`, id generated with `Guid.CreateVersion7()` for insert locality) carries: `Type`, `Content`, `OccurredOn`, `ProcessedOn`, `AttemptCount`, `Error`, `TenantId`, `CorrelationId`, plus two fields beyond the backlog sketch — `NextAttemptOn` and `LockedUntil` — needed by D5/D6. Behaviour: `MarkProcessed(now)`, `MarkFailed(now, error, nextAttemptOn)`, `Release()`, `IsDeadLetter(maxAttempts)`.

*Why not store unhandled events:* they would be claimed, find no handler, and be marked processed — pure write amplification. The cost is that a handler added later does not see past events; acceptable, and identical to in-process mode.

### D3. Serialization: `System.Text.Json`, short type name, handler-derived allow-list

- `IDomainEventTypeRegistry` (Application) is built in `AddRaftelApplication` during the existing handler scan. It maps each handled event type ⇄ its stored name `"{FullName}, {AssemblyName}"` (no version, culture or key token) and lists the handler implementation types per event type.
- `IOutboxSerializer` (Application; implementation in Infrastructure) returns `Result<IDomainEvent>` on read. A name absent from the registry is a failure — `Type.GetType` is never called on database content.
- Dedicated `JsonSerializerOptions` with a contract modifier that assigns get-only auto-properties through their compiler-generated backing field, so `OccurredOn { get; } = UtcNow` round-trips to the original instant instead of silently becoming "time of deserialization".

*Alternatives:* allow-list = every `IDomainEvent` in the scanned assemblies (backlog wording) — wider than necessary given D2, and "has a handler" is already computed. Requiring events to use `init` setters — rejected: existing events would deserialize with wrong timestamps without any error.

Limitation to document: handlers registered by hand, outside `RegisterServicesFromAssembly`, are invisible to the registry.

### D4. Processor = thin hosted loop + testable batch runner

- `OutboxProcessor` (`BackgroundService`): `PeriodicTimer` on `PollingInterval`; loops again immediately while a cycle returns a full batch; catches and logs infrastructure exceptions so a database blip does not kill the host.
- `OutboxBatchRunner`: one cycle — claim, dispatch, record outcome. Integration tests call it directly with a `FakeTimeProvider`.
- `OutboxMessageDispatcher`: delivers one message (D7).

Claimed messages are tracked entities in a store-owned DI scope separate from every handler scope; outcomes are applied through the domain methods and saved by `IOutboxStore`.

### D5. Claim by lease, in one provider-specific statement

`IOutboxStore.ClaimAsync` runs a single atomic statement that selects eligible rows oldest-first, sets `LockedUntil = now + LockDuration`, **increments `AttemptCount`**, and returns the rows:

- SQL Server: CTE with `TOP (@n) … ORDER BY OccurredOn` over `WITH (UPDLOCK, READPAST, ROWLOCK)`, `UPDATE … OUTPUT inserted.*`.
- PostgreSQL: `UPDATE … WHERE Id IN (SELECT … ORDER BY "OccurredOn" LIMIT @n FOR UPDATE SKIP LOCKED) RETURNING *`.

Eligible = `ProcessedOn IS NULL AND AttemptCount < MaxAttempts AND (NextAttemptOn IS NULL OR NextAttemptOn <= now) AND (LockedUntil IS NULL OR LockedUntil < now)`. Table and column identifiers come from the EF model metadata, not string literals, so consumer naming conventions are respected. The result is materialised with `FromSqlRaw(...).IgnoreQueryFilters()` without further composition.

*Why increment at claim rather than in `MarkFailed` (backlog sketch):* a message that kills the process never reaches `MarkFailed`; counting at claim time is what lets a crash-looping "poison" message reach the dead-letter state.

*Why a lease rather than holding row locks for the batch:* no long transaction spanning handler execution, and a crashed instance's claim expires by itself.

*Lease overrun:* before starting each message the runner checks the remaining lease; if it is nearly spent (or shutdown was requested) the rest of the batch is `Release()`d — `LockedUntil` cleared and the claim's attempt handed back — instead of being processed on a stale lease.

### D6. Retry and dead letter

Pure `OutboxRetrySchedule` (Application): `delay(attempt) = min(BaseRetryDelay × 2^(attempt−1), MaxRetryDelay)`. On failure: `MarkFailed` stores the (truncated) error and `NextAttemptOn = now + delay`, clears the lease. Dead letter is derived, not a column: `ProcessedOn IS NULL AND AttemptCount >= MaxAttempts`; the claim predicate already excludes it. `IOutboxStore.GetDeadLettersAsync` exposes them. Unknown type and malformed content go through the same path (a redeploy can make an unknown type known again).

`MaxRetryDelay` (5 min) and `LockDuration` (1 min) are additions to the backlog's `OutboxOptions`.

### D7. Delivery: one scope and one transaction per (message, handler), with receipts

For each handler implementation type listed by the registry for the event:

1. Create a DI scope; `ICurrentTenant.Change(message.TenantId)`; `ICorrelationScope.Begin(message.CorrelationId)`; logger scope.
2. `IUnitOfWork.BeginTransactionAsync`.
3. `IIdempotencyStore.HasCompletedAsync(messageId, handlerName)` → skip if true.
4. Invoke the handler.
5. `IIdempotencyStore.MarkCompletedAsync` — inserts a receipt through the same `DbContext` and saves.
6. Commit. Any exception ⇒ rollback, the message is failed, remaining handlers are not attempted this round.

Receipts are an internal Infrastructure entity (`OutboxHandlerReceipt`, key `(MessageId, Handler)`, FK to the message with cascade delete). The composite key also stops two instances that both hold a (stale) lease from both committing the same handler. After all handlers complete the message is `MarkProcessed`.

*Why per-handler scope:* a failed handler leaves dirty tracked entities; a fresh `DbContext` per handler avoids leaking them into the next one. *Why always on rather than optional (backlog: "optional `IIdempotencyStore`"):* without it, a message with two handlers re-runs the first whenever the second fails — a correctness trap that should not be the default. The abstraction stays public so it can be replaced.

Non-database side effects remain at-least-once; handlers performing them must be idempotent. This is documented on `IDomainEventHandler`.

Handlers that persist aggregates raising new events get those events captured by the same `OutboxWriteInterceptor`, inside the handler's transaction.

### D8. Ambient context: tenant column and `ICorrelationScope`

- The message's `TenantId` is filled explicitly from `ICurrentTenant` at capture. Because the property name matches the shadow-property convention, the entity receives the global tenant filter; every store query therefore uses `IgnoreQueryFilters()` (the processor has no current tenant anyway, but this must not depend on that). `TenantInterceptor` setting it on insert yields the same value.
- New `ICorrelationScope { IDisposable Begin(string? correlationId); }` in Application, implemented beside `CorrelationContext` with an `AsyncLocal` override that `CorrelationContext.CorrelationId` consults first. `ICorrelationContext` stays read-only.

*Alternative:* add a setter to `ICorrelationContext` — rejected; it would let any request component rewrite the request's id.

### D9. Persistence mapping

`OutboxMessageConfiguration` + receipt configuration in `Data/Configuration/Outbox/`. Indexes: pending lookup on `OccurredOn` filtered by `ProcessedOn IS NULL`, and `ProcessedOn` for cleanup. The filter text differs per provider (`[ProcessedOn]` vs `"ProcessedOn"`), so it is applied in `RaftelDbContext.OnModelCreating` based on the active provider. `Type` 512 chars, `CorrelationId` 128, `Error` 4000 (truncated before assignment), `Content` unbounded. Outbox entity types are added to the audit exclusion set in `ChangeSnapshotExtractor`.

The entities are mapped regardless of `Enabled`, so the model — and consumer migrations — do not vary with a runtime flag.

### D10. Configuration and cleanup

`OutboxOptions` in `Raftel.Application`, registered as a singleton instance via a new trailing optional parameter `Action<OutboxOptions>? configureOutbox` on `AddRaftelData`. `Validate()` runs at registration when enabled and throws `InvalidOperationException` naming the offending setting. Enabling without `AddRaftelApplication` (no registry) fails at startup rather than silently dropping events.

`OutboxCleanupService` (own `BackgroundService`, hourly — backlog 16 does not exist yet) calls `IOutboxStore.DeleteProcessedBeforeAsync(now − RetentionPeriod)` using `ExecuteDeleteAsync`; receipts go with the cascade.

## Risks / Trade-offs

- [Handlers lose `HttpContext`/`ICurrentUser` when the outbox is enabled] → Documented as the main behavioural difference; tenant and correlation are restored, the user is not.
- [A handler slower than `LockDuration` can be delivered twice] → Per-message lease check + `Release()` (D5), receipts make database effects single-apply (D7), and guidance to size `LockDuration` above the slowest handler.
- [`Enabled` turned off while messages are pending strands them] → Documented drain procedure: stop producing, let the processor empty the table, then disable.
- [Events stop being delivered in order] → Stated non-goal; documented.
- [Raw claim SQL is the only provider-specific code] → Both statements covered by the two-processor concurrency test on SQL Server and PostgreSQL via Testcontainers.
- [Backing-field assignment in the serializer relies on a compiler naming convention] → Covered by a round-trip test on a get-only `OccurredOn`; falls back to normal STJ behaviour where no backing field exists.
- [New tables appear for consumers who never enable the outbox] → Accepted over a model that changes shape with configuration; called out in the proposal and docs.
- [Polling adds up to `PollingInterval` latency and a small constant query load] → Interval configurable; immediate re-poll on full batches.

## Migration Plan

1. Upgrade; add an EF migration (two tables, two indexes). Nothing else changes while `Enabled` is false.
2. Make handlers idempotent for non-database effects and free of `HttpContext`/`ICurrentUser` dependencies.
3. Set `configureOutbox: o => o.Enabled = true` and deploy.

Rollback: drain pending messages, set `Enabled = false`. The tables can stay.

## Open Questions

- Should a later change expose dead-letter requeue and an outbox-backlog health check (backlog 09 integration)? Not needed for this change's acceptance criteria.
