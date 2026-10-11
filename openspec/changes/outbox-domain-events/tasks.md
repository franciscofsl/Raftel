# Tasks

## 1. Domain model

- [x] 1.1 (test) Add `tests/Raftel.Domain.Tests/Outbox/OutboxMessageTests.cs`: creation sets type, content, occurred-on, tenant, correlation and starts pending with `AttemptCount == 0`; verify the tests compile and fail before 1.3.
- [x] 1.2 (test) Cover transitions in the same file: `MarkProcessed(now)` sets `ProcessedOn` and clears error/lease; `MarkFailed(now, error, nextAttemptOn)` stores a truncated error and `NextAttemptOn`, clears `LockedUntil`, leaves `AttemptCount` untouched; `Release()` clears the lease and hands back one attempt (never below 0); `IsDeadLetter(maxAttempts)` is true only when unprocessed and at/over the limit.
- [x] 1.3 Implement `src/Raftel.Domain/Outbox/OutboxMessage.cs` and `Outbox/ValueObjects/OutboxMessageId.cs` (`Guid.CreateVersion7()`), per design D2; verify with `dotnet test tests/Raftel.Domain.Tests` green.

## 2. Application abstractions and options

- [x] 2.1 (test) `tests/Raftel.Application.UnitTests/Outbox/OutboxOptionsTests.cs`: documented defaults (50 / 5 s / 5 / 10 s / 5 min / 1 min / 7 days, `Enabled == false`); `Validate()` throws `InvalidOperationException` naming the setting for non-positive batch size, attempts or durations and for `MaxRetryDelay < BaseRetryDelay`.
- [x] 2.2 (test) `OutboxRetryScheduleTests.cs`: attempt 1 ⇒ base delay, each further attempt doubles, result never exceeds `MaxRetryDelay`, large attempt numbers do not overflow.
- [x] 2.3 Implement `src/Raftel.Application/OutboxOptions.cs` and `Abstractions/Outbox/OutboxRetrySchedule.cs`; verify 2.1–2.2 pass.
- [x] 2.4 (test) `DomainEventTypeRegistryTests.cs` + extend `DependencyInjectionTests.cs`: after `AddRaftelApplication` scanning an assembly, the registry resolves a handled event type to `"{FullName}, {AssemblyName}"` and back, lists every handler implementation type for it, and reports an unhandled event type / unknown name as not registered.
- [x] 2.5 Add `Abstractions/Outbox/IDomainEventTypeRegistry.cs` and its implementation; build and register it as a singleton inside `RegisterHandlers` in `src/Raftel.Application/DependencyInjection.cs`; verify 2.4 passes.
- [x] 2.6 Add `Abstractions/Outbox/IOutboxStore.cs` (claim, save, dead-letter query, delete-processed-before), `IOutboxSerializer.cs` (`Serialize` / `Result<IDomainEvent> Deserialize`), `IIdempotencyStore.cs`, and `Abstractions/ICorrelationScope.cs`, all XML-documented; verify `dotnet build src/Raftel.Application` succeeds and `tests/Raftel.ArchitectureTests` stays green (no EF reference in Application).
- [x] 2.7 Update the `<remarks>` on `IDomainEventHandler<TEvent>` to describe both modes (in-process vs outbox: separate scope, no `HttpContext`, at-least-once, per-handler transaction and receipt); verify by reading the generated XML doc in the build output.

## 3. Correlation scope

- [x] 3.1 (test) Extend `tests/Raftel.Infrastructure.Tests/Correlation/`: inside `ICorrelationScope.Begin("abc")` `ICorrelationContext.CorrelationId` returns `"abc"`; after dispose it returns the request/generated value again; nested scopes restore the outer value; `Begin(null)` does not override.
- [x] 3.2 Implement the `AsyncLocal` override in `src/Raftel.Infrastructure/Correlation/` and make `CorrelationContext` consult it first; register `ICorrelationScope` in `AddRaftelData`; verify 3.1 and the existing `CorrelationIdTests` functional tests pass.

## 4. Persistence mapping

- [x] 4.1 Add `Data/Configuration/Outbox/OutboxMessageConfiguration.cs` and the internal `Outbox/OutboxHandlerReceipt.cs` + configuration (composite key, cascade FK), `DbSet<OutboxMessage>` on `RaftelDbContext`, and the provider-specific filtered pending index in `OnModelCreating` (design D9); verify with an `Infrastructure.Tests` test, run on SQL Server and PostgreSQL, that `EnsureCreated` succeeds and an `OutboxMessage` round-trips through the context.
- [x] 4.2 Add the outbox entity types to the exclusion set in `Data/Auditing/ChangeSnapshotExtractor.cs`; verify with a test that saving an `OutboxMessage` produces no `AuditLog` entry for it.
- [x] 4.3 If `BackgroundService` does not resolve from `Raftel.Infrastructure`, add `Microsoft.Extensions.Hosting.Abstractions` to `Directory.Packages.props` and the csproj; verify `dotnet build Raftel.sln` succeeds.

## 5. Serializer

- [x] 5.1 (test) `tests/Raftel.Infrastructure.Tests/Outbox/OutboxSerializerTests.cs`: round trip of a registered event with a strongly-typed id yields an equal event; a get-only `OccurredOn { get; } = UtcNow` keeps its original instant; unknown type name ⇒ failed `Result` naming the type and no instance created; malformed JSON ⇒ failed `Result`, no exception; stored type name contains no version/culture/token.
- [x] 5.2 Implement `src/Raftel.Infrastructure/Outbox/OutboxSerializer.cs` with dedicated `JsonSerializerOptions` and the backing-field contract modifier (design D3); verify 5.1 passes.

## 6. Capture (write side)

- [x] 6.1 (test) `tests/Raftel.Infrastructure.Tests/Outbox/OutboxWriteInterceptorTests.cs` (both providers), outbox enabled: saving an aggregate with a handled event stores exactly one pending message with the event's `OccurredOn`, current tenant and correlation id, and clears the aggregate's events; three events ⇒ three rows; an event with no handler ⇒ no row but events cleared; the handler is **not** invoked by the save.
- [x] 6.2 (test) Same file: inside `BeginTransactionAsync` + rollback, no message row survives; an event that cannot be serialized makes `SaveChanges` throw and persists neither the aggregate change nor a message; the synchronous `SaveChanges` path behaves like the async one.
- [x] 6.3 Implement `src/Raftel.Infrastructure/Data/Interceptors/OutboxWriteInterceptor.cs` (sync + async `SavingChanges`); verify 6.1–6.2 pass.
- [x] 6.4 Add the `configureOutbox` parameter to `AddRaftelData`, register `OutboxOptions` as a singleton (validated when enabled), and select `OutboxWriteInterceptor` vs `DomainEventsDispatchInterceptor` in `AddDataAccess` (design D1); extend the demo's `AddSampleInfrastructure` test helper to pass the callback through. Verify: existing `DomainEventsDispatchInterceptorTests` pass unmodified, and a new test asserts that with default options no outbox row is written and the handler runs in-process.
- [x] 6.5 (test + impl) Startup validation: enabling with an invalid option, or enabling without `AddRaftelApplication` (no registry), fails at registration/startup with a message naming the problem; verify with unit tests.

## 7. Store and claiming

- [x] 7.1 (test) `tests/Raftel.Infrastructure.Tests/Outbox/OutboxStoreTests.cs` (both providers) with `FakeTimeProvider`: claim returns oldest-first up to the batch size, sets `LockedUntil`, increments `AttemptCount`; skips processed rows, rows with future `NextAttemptOn`, rows with an unexpired lease and rows at `MaxAttempts`; a row with an expired lease is claimable again; claiming ignores the current tenant (rows of every tenant are returned while a tenant is set).
- [x] 7.2 (test) Concurrency: 200 pending messages, two stores claiming in parallel loops on separate connections ⇒ the union of claimed ids has no duplicates and equals all 200. Run on SQL Server and PostgreSQL.
- [x] 7.3 Implement `src/Raftel.Infrastructure/Outbox/OutboxStore.cs` with the two provider claim statements built from EF model metadata (design D5), `SaveAsync`, `GetDeadLettersAsync`, `DeleteProcessedBeforeAsync`; verify 7.1–7.2 pass.
- [x] 7.4 (test + impl) `EfIdempotencyStore`: `HasCompletedAsync` false then true after `MarkCompletedAsync`; a duplicate `(message, handler)` receipt fails; a receipt written inside a rolled-back transaction is not visible. Verify on both providers.

## 8. Delivery and processor

- [x] 8.1 (test) `tests/Raftel.Infrastructure.Tests/Outbox/OutboxBatchRunnerTests.cs`: a pending message is delivered once to every registered handler with an event equal to the original and is marked processed; a second cycle invokes nothing.
- [x] 8.2 (test) Failure path with `FakeTimeProvider`: handler throws ⇒ unprocessed, `AttemptCount == 1`, error stored, not re-claimed before the base delay, re-claimed after it; delays double and cap; after `MaxAttempts` the message is returned by `GetDeadLettersAsync` and never claimed again; a failing message does not prevent another message in the batch from being processed.
- [x] 8.3 (test) Idempotency: two handlers, second throws ⇒ on retry only the second runs; a handler that writes then throws leaves no row and no receipt; all handlers completed but message left unprocessed (simulated crash) ⇒ redelivery invokes nothing and marks it processed.
- [x] 8.4 (test) Ambient context: handler observes the message's tenant (and reads tenant-filtered data accordingly) and correlation id; a null-tenant message runs with no tenant; tenant A then tenant B in one cycle do not leak; no tenant is current after the cycle.
- [x] 8.5 (test) Unknown type / malformed content ⇒ failed attempt with descriptive error, no handler invoked, rest of batch continues; a handler that saves an aggregate raising a new handled event produces a new pending message delivered in a later cycle.
- [x] 8.6 Implement `Outbox/OutboxMessageDispatcher.cs` (scope + tenant + correlation + transaction + receipt per handler, design D7) and `Outbox/OutboxBatchRunner.cs` (claim, lease check with `Release()`, outcome recording, structured log entry per attempt and error-level entry on dead letter); verify 8.1–8.5 pass on both providers.
- [x] 8.7 (test + impl) `Outbox/OutboxProcessor.cs` (`BackgroundService`, `PeriodicTimer`, immediate re-poll on a full batch, swallow-and-log infrastructure exceptions): with a cancelled stopping token it exits promptly, unstarted messages of the batch are released and an interrupted message stays unprocessed; an exception thrown by the store does not terminate the loop. Register it as a hosted service only when enabled; verify with a DI test that it is absent when disabled.
- [x] 8.8 (test) Logging: using a capturing `ILoggerProvider`, a successful delivery logs message id, event type, attempt, outcome, tenant and correlation id; exhausting attempts logs at `Error`.

## 9. Retention cleanup

- [x] 9.1 (test) `OutboxCleanupTests.cs` (both providers) with `FakeTimeProvider`: a message processed before the cutoff is deleted together with its receipts; one processed within retention stays; pending and dead-letter messages older than retention stay.
- [x] 9.2 Implement `Outbox/OutboxCleanupService.cs` and register it only when enabled; verify 9.1 passes and the DI test from 8.7 also asserts its absence when disabled.

## 10. End-to-end, demo and documentation

- [x] 10.1 `tests/Raftel.Application.IntegrationTests/OutboxTests.cs`, outbox enabled, through the real command pipeline with `TransactionMiddleware` + `UnitOfWorkMiddleware`: a command whose handler always throws still returns success and persists; a command that returns a failed `Result` leaves no message; after running one batch the handler has executed in the command's tenant.
- [x] 10.2 No-regression test in the same project with the outbox disabled: handler runs before the command returns, a throwing handler propagates, and the outbox table stays empty.
- [x] 10.3 Add the demo migration for the two tables and indexes under `demo/Raftel.Demo.Infrastructure/Data/Migrations`; verify `dotnet ef migrations has-pending-model-changes` reports none and the functional test suite still passes.
- [x] 10.4 Write `docs/outbox.md` (enabling, options table, guarantees: at-least-once + receipts, no ordering, no `HttpContext`/current user, sizing `LockDuration`, querying dead letters, required migration, drain-before-disable) and link it from `docs/toc.yml` and `docs/domain-events-plan.md`; verify the option names and defaults in the doc match `OutboxOptions`.
- [x] 10.5 Update `src/Raftel.Domain/CLAUDE.md`, `src/Raftel.Application/CLAUDE.md`, `src/Raftel.Infrastructure/CLAUDE.md` with the new `Outbox` areas, and set `docs/backlog/07-outbox-domain-events.md` to done with the deviations from its sketch (attempt counted at claim, extra fields/options, always-on receipts); verify each file mentions the outbox.
- [x] 10.6 Run `dotnet build Raftel.sln` and `dotnet test` for the whole solution; verify zero warnings-as-errors and all projects green, and tick the six acceptance criteria of issue #131 against the tests that prove them.

## Workflow follow-up

- Archive the change after review and merge.
