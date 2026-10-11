# Spec Delta

## Purpose

Guarantees that a domain event raised by a committed state change is durably recorded and eventually delivered to its handlers, without letting handler failures or handler latency affect the command that raised it.

## ADDED Requirements

### Requirement: Outbox is opt-in and disabled by default

The outbox SHALL be disabled unless the application explicitly enables it. While disabled, domain events SHALL be dispatched in-process exactly as before this capability existed, and no background processing SHALL run.

#### Scenario: Default configuration keeps in-process dispatch

- **WHEN** an application does not configure the outbox and a command persists an aggregate that raised a domain event
- **THEN** the event's handlers run in-process before the command returns
- **AND** no outbox message is stored

#### Scenario: Disabled mode still propagates handler failures

- **WHEN** the outbox is disabled and a domain event handler throws
- **THEN** the exception propagates to the caller of the save, as it did before

#### Scenario: Disabled mode runs no background work

- **WHEN** the outbox is disabled
- **THEN** no outbox processor and no retention cleanup run

### Requirement: Events are captured atomically with the state change

When the outbox is enabled, every domain event raised by a persisted aggregate that has at least one registered handler SHALL be stored as an outbox message in the same atomic unit as the aggregate's state change. The aggregate's pending events SHALL be cleared once captured.

#### Scenario: Committed change stores its events

- **WHEN** a command persists an aggregate that raised a domain event and the save commits
- **THEN** exactly one pending outbox message exists for that event
- **AND** the aggregate has no pending domain events

#### Scenario: Rolled-back change stores nothing

- **WHEN** a command raises a domain event and its transaction is rolled back
- **THEN** no outbox message exists for that event

#### Scenario: Several events from one save

- **WHEN** one save persists aggregates that raised three domain events with registered handlers
- **THEN** three outbox messages are stored, each recording the time its event occurred

#### Scenario: Event without a handler is not stored

- **WHEN** a persisted aggregate raised a domain event that has no registered handler
- **THEN** no outbox message is stored for it
- **AND** the aggregate's pending events are still cleared

#### Scenario: Event that cannot be serialized fails the save

- **WHEN** a domain event with a registered handler cannot be serialized
- **THEN** the save fails with an error naming the event type
- **AND** neither the state change nor any outbox message is persisted

### Requirement: Handlers do not run inside the command

When the outbox is enabled, domain event handlers SHALL NOT be invoked as part of the save that raised the event. A handler's failure or duration SHALL NOT affect the outcome or response time of the command.

#### Scenario: Failing handler does not fail the command

- **WHEN** a command raises an event whose handler always throws
- **THEN** the command returns a successful result
- **AND** the state change is persisted

#### Scenario: Handler has not run when the command returns

- **WHEN** a command raising an event returns and the processor has not yet polled
- **THEN** the event's handler has not been invoked

### Requirement: Pending messages are delivered to every registered handler

The outbox processor SHALL periodically take pending messages, oldest first, up to the configured batch size, and invoke every handler registered for each event's type. A message SHALL be marked processed, with the processing time, once all its handlers have completed.

#### Scenario: Successful delivery

- **WHEN** a pending message exists and the processor runs
- **THEN** each registered handler for that event type receives an event equal to the one originally raised
- **AND** the message is marked processed

#### Scenario: Processed message is not redelivered

- **WHEN** the processor runs again after a message was marked processed
- **THEN** no handler is invoked for that message

#### Scenario: Batch size bounds one cycle

- **WHEN** more pending messages exist than the configured batch size
- **THEN** a single processing cycle claims at most the batch size
- **AND** the remainder are delivered in later cycles

#### Scenario: Events raised by a handler are captured

- **WHEN** a handler invoked by the processor persists an aggregate that raises a further domain event
- **THEN** that event is stored as a new outbox message and delivered in a later cycle

### Requirement: Tenant and correlation are restored for handlers

Each outbox message SHALL record the tenant and correlation identifier in effect when its event was captured. The processor SHALL invoke the handlers with that tenant as the current tenant and that correlation identifier as the current correlation identifier, and SHALL NOT leak either into the handling of any other message.

#### Scenario: Handler runs in the originating tenant

- **WHEN** an event is captured while tenant A is current and the processor later delivers it
- **THEN** the handler observes tenant A as the current tenant
- **AND** data it reads and writes is scoped to tenant A

#### Scenario: Handler sees the originating correlation identifier

- **WHEN** an event is captured during a request with correlation identifier X
- **THEN** the handler observes X as the current correlation identifier

#### Scenario: Event captured with no tenant

- **WHEN** an event is captured while no tenant is current
- **THEN** the handler runs with no current tenant

#### Scenario: No leakage between messages

- **WHEN** the processor delivers a message for tenant A followed by a message for tenant B in the same cycle
- **THEN** the second handler observes tenant B, and after the cycle no tenant is current

### Requirement: Failed deliveries are retried with capped exponential backoff

When a handler fails, the message SHALL remain unprocessed, its attempt count SHALL reflect the attempt, and the failure reason SHALL be recorded. The message SHALL NOT be retried before a delay that doubles with each attempt, starting at the configured base delay and never exceeding the configured maximum delay.

#### Scenario: First failure schedules a retry

- **WHEN** a handler throws on a message's first delivery
- **THEN** the message stays unprocessed with an attempt count of 1 and the error recorded
- **AND** it is not claimed again until the base retry delay has elapsed

#### Scenario: Delay grows and is capped

- **WHEN** a message keeps failing
- **THEN** each retry delay is twice the previous one until it reaches the configured maximum, after which it stays at the maximum

#### Scenario: Retry succeeds

- **WHEN** a message failed once and its handler succeeds on the next attempt
- **THEN** the message is marked processed

#### Scenario: One failing message does not block the batch

- **WHEN** a batch contains a message whose handler throws and another whose handler succeeds
- **THEN** the second message is marked processed in the same cycle

### Requirement: Exhausted messages become queryable dead letters

A message whose attempt count reaches the configured maximum without being processed SHALL become a dead letter: it SHALL no longer be delivered, SHALL remain stored with its last error, and SHALL be retrievable through the outbox's dead-letter query.

#### Scenario: Attempts exhausted

- **WHEN** a message's handler fails on every attempt up to the configured maximum
- **THEN** the processor stops delivering it
- **AND** the dead-letter query returns it with its attempt count and last error

#### Scenario: Dead letters survive cleanup

- **WHEN** retention cleanup runs and a dead letter is older than the retention period
- **THEN** the dead letter is still stored

### Requirement: A message is delivered by one processor at a time

When several processor instances run against the same database, a pending message SHALL be claimed by at most one of them at a time. A claim SHALL expire after the configured lock duration, so that a message claimed by an instance that stopped is delivered by another.

#### Scenario: Two processors, no duplicate delivery

- **WHEN** two processor instances poll concurrently while a set of pending messages exists
- **THEN** each message's handler is invoked exactly once across both instances

#### Scenario: Claim of a crashed instance expires

- **WHEN** an instance claims a message and stops before finishing it
- **THEN** after the lock duration another instance claims and delivers the message

#### Scenario: Crash-looping message is eventually dead-lettered

- **WHEN** a message causes the processor to terminate on every attempt before any outcome is recorded
- **THEN** its attempt count still increases with each claim
- **AND** it becomes a dead letter once the maximum is reached

### Requirement: A handler that completed is not re-run on redelivery

Delivery is at-least-once. The framework SHALL record, atomically with a handler's own database writes, that the handler completed for a given message, and SHALL skip that handler when the same message is delivered again.

#### Scenario: Partial failure across handlers

- **WHEN** a message has two handlers, the first completes and the second throws, and the message is retried
- **THEN** only the second handler is invoked on the retry

#### Scenario: Handler writes and completion record are atomic

- **WHEN** a handler writes to the database and then throws
- **THEN** its database writes are not persisted
- **AND** no completion is recorded for it

#### Scenario: Redelivery after a crash before marking processed

- **WHEN** all handlers completed but the process stopped before the message was marked processed
- **THEN** on redelivery no handler is invoked again and the message is marked processed

### Requirement: Only allow-listed event types are materialised

The outbox SHALL deserialize a stored message only when its recorded type matches a domain event type that has a registered handler in the running application. Any other recorded type SHALL be treated as a failed delivery without loading or instantiating the named type.

#### Scenario: Round trip of a registered event

- **WHEN** an event with a registered handler is captured and later delivered
- **THEN** the delivered event equals the captured one, including the time it occurred

#### Scenario: Unknown type in a stored message

- **WHEN** a stored message names a type that is not a registered domain event type
- **THEN** no handler is invoked and no instance of the named type is created
- **AND** the message is recorded as failed with an error identifying the unknown type

#### Scenario: Stored type name is version-independent

- **WHEN** a message was stored by an earlier version of the event's assembly
- **THEN** it is still delivered after the assembly version changes

#### Scenario: Malformed content

- **WHEN** a stored message's content cannot be deserialized into its recorded type
- **THEN** the message is recorded as failed with the error, and the processor continues with other messages

### Requirement: Processed messages are purged after the retention period

A background cleanup SHALL delete processed messages, together with their handler completion records, once they have been processed for longer than the configured retention period. Unprocessed messages SHALL never be deleted by cleanup.

#### Scenario: Old processed message is deleted

- **WHEN** cleanup runs and a message was processed longer ago than the retention period
- **THEN** the message and its handler completion records are deleted

#### Scenario: Recent processed message is kept

- **WHEN** cleanup runs and a message was processed within the retention period
- **THEN** the message is still stored

#### Scenario: Pending message is kept regardless of age

- **WHEN** cleanup runs and an unprocessed message is older than the retention period
- **THEN** the message is still stored

### Requirement: Outbox behaviour is configurable with safe defaults

The application SHALL be able to configure batch size, polling interval, maximum attempts, base and maximum retry delay, lock duration and retention period. Defaults SHALL be: batch size 50, polling interval 5 seconds, 5 attempts, base delay 10 seconds, maximum delay 5 minutes, lock duration 1 minute, retention 7 days. Invalid values SHALL be rejected at startup.

#### Scenario: Defaults apply when only enabling

- **WHEN** an application enables the outbox without setting anything else
- **THEN** the documented defaults are in effect

#### Scenario: Invalid configuration fails fast

- **WHEN** an application enables the outbox with a batch size, attempt limit or duration that is zero or negative, or a maximum retry delay below the base delay
- **THEN** application startup fails with an error naming the invalid setting

### Requirement: Processing stops cleanly on shutdown

When the host shuts down, the processor SHALL stop claiming new messages and stop promptly. A message whose delivery was interrupted SHALL remain unprocessed and be delivered again later.

#### Scenario: Shutdown during a cycle

- **WHEN** the host requests shutdown while a message is being delivered
- **THEN** the processor stops without marking that message processed
- **AND** the message is delivered again after its claim expires

### Requirement: Each delivery attempt is traceable

Every delivery attempt SHALL emit a structured log entry carrying the message identifier, event type, attempt number, outcome, tenant and correlation identifier. A message becoming a dead letter SHALL be logged at error level.

#### Scenario: Successful delivery is logged

- **WHEN** a message is delivered successfully
- **THEN** a log entry records its identifier, event type, attempt number, a success outcome and its correlation identifier

#### Scenario: Dead letter is logged as an error

- **WHEN** a message exhausts its attempts
- **THEN** an error-level log entry records its identifier, event type and last error
