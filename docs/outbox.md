# Transactional Outbox for Domain Events

By default Raftel dispatches domain events **in-process**, right after `SaveChanges`. That is simple, but a
handler that throws fails a command that already persisted, the event is lost, and every handler's latency is
added to the HTTP response. The **outbox** fixes this: each event is stored in the same transaction as the
state change that raised it, and a background processor delivers it to its handlers later, with retries.

The outbox is **opt-in**. With the default options nothing below applies and behaviour is unchanged.

## Enabling it

```csharp
services.AddRaftelApplication(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(MyHandler).Assembly);   // event handlers are discovered here
    // ...
});

services.AddRaftelData<AppDbContext>(configuration, configureOutbox: outbox =>
{
    outbox.Enabled = true;
    outbox.BatchSize = 100;                    // optional, see the table below
});
```

`AddRaftelApplication` must be called as well: only events that have a handler found by that scan are ever
stored or delivered. Enabling the outbox without it fails when the host builds its hosted services.

**Schema.** The framework model now contains two tables (`OutboxMessages`, `OutboxHandlerReceipts`) whether
or not the outbox is enabled. If you manage the schema with EF migrations, add a migration after upgrading.

## Options

| Option | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | Route domain events through the outbox. |
| `BatchSize` | `50` | Messages claimed per processing cycle. |
| `PollingInterval` | `5 s` | Wait between polls. A full batch is followed by an immediate new poll. |
| `MaxAttempts` | `5` | Delivery attempts before a message becomes a dead letter. |
| `BaseRetryDelay` | `10 s` | Delay after the first failure; doubles with every further attempt. |
| `MaxRetryDelay` | `5 min` | Upper bound of the retry delay. |
| `LockDuration` | `1 min` | How long a processor owns a claimed message. Size it **above your slowest handler**. |
| `RetentionPeriod` | `7 days` | Processed messages older than this are deleted. |

Invalid values (zero or negative, or `MaxRetryDelay` below `BaseRetryDelay`) fail at registration, naming the
setting.

## What you get

- **No lost events.** If the command's transaction commits, the event is stored; if it rolls back, it is not.
- **Handlers cannot fail the command.** They run later, in the background.
- **Tenant and correlation id are restored.** The handler sees the tenant and `ICorrelationContext.CorrelationId`
  of the request that raised the event. Reads and writes it makes are scoped to that tenant.
- **Retries and dead letters.** A failing message is retried with capped exponential backoff. After
  `MaxAttempts` it stays in the table as a dead letter and is logged at error level. Query them with
  `IOutboxStore.GetDeadLettersAsync()`.
- **Several instances are safe.** A message is leased to one processor at a time; the lease of a crashed
  instance expires after `LockDuration` and another instance picks the message up.
- **A handler that completed is not re-run.** Each handler runs in its own transaction together with a
  receipt that it completed. If the message is redelivered (another handler failed, or the process died
  before the message was marked processed) only the handlers without a receipt run again.

## What you must know

- **Delivery is at-least-once and unordered.** Retries and several instances can reorder events. Do not assume
  order.
- **Only database writes are atomic with the receipt.** Sending an email or calling an HTTP API from a handler
  can still happen more than once. Make those effects idempotent.
- **No `HttpContext`, no current user.** Handlers run outside the request. Do not depend on
  `ICurrentUser` or `IHttpContextAccessor` in handlers.
- **Events without a handler are not stored**, just as they were no-ops before. A handler added later does not
  see past events.
- **Handlers registered by hand** (not found by `RegisterServicesFromAssembly`) run, but their event type is
  only deliverable if some scanned handler exists for the same event type.
- **Handlers can raise events.** A handler that persists an aggregate raising another event stores a new
  message, delivered in a later cycle.

## Turning it off again

Messages already stored are only delivered while the outbox is enabled. Before disabling it, stop producing
events, let the processor drain the table, then set `Enabled = false`. The tables can stay.

## Cleanup

A background service deletes processed messages, with their receipts, older than `RetentionPeriod`, once an
hour. Unprocessed messages and dead letters are never deleted automatically.
