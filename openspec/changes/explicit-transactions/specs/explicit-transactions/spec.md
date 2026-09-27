## Purpose

Gives every command a single, explicit atomic boundary: all database writes made while handling one command — including those made by domain event handlers and auditing — either all persist or none do.

## ADDED Requirements

### Requirement: Unit of work exposes an explicit transaction boundary

The unit of work SHALL be able to begin an explicit transaction and report whether one is already active. The transaction handle SHALL expose commit, rollback and asynchronous disposal, and SHALL NOT expose any persistence-framework type to consumers in the application layer.

#### Scenario: Beginning a transaction when none is active

- **WHEN** a caller begins a transaction on a unit of work with no active transaction
- **THEN** a transaction handle is returned
- **AND** the unit of work reports that a transaction is active

#### Scenario: No transaction active by default

- **WHEN** a unit of work has not begun a transaction
- **THEN** it reports that no transaction is active

#### Scenario: Disposing the transaction handle

- **WHEN** a transaction handle is disposed after being committed or rolled back
- **THEN** disposal completes without error
- **AND** the unit of work reports that no transaction is active

#### Scenario: Disposing an uncommitted transaction

- **WHEN** a transaction handle is disposed without having been committed or rolled back
- **THEN** the transaction is rolled back and no writes made within it are persisted

### Requirement: Commands execute inside a single atomic boundary

Command handling SHALL run inside one transaction that is committed only when the command produces a successful result. A failed result SHALL roll the transaction back, and an exception SHALL roll it back and propagate unchanged.

#### Scenario: Successful command commits once

- **WHEN** a command completes with a successful result
- **THEN** the transaction is committed exactly once
- **AND** no rollback occurs

#### Scenario: Failed result rolls back

- **WHEN** a command completes with a failed result
- **THEN** the transaction is rolled back
- **AND** the failed result is returned to the caller unchanged

#### Scenario: Exception rolls back and propagates

- **WHEN** an exception is thrown while handling a command
- **THEN** the transaction is rolled back
- **AND** the original exception propagates to the caller

#### Scenario: Partial writes are never persisted

- **WHEN** a command writes one aggregate, persists it, then fails while writing a second aggregate
- **THEN** no rows written by that command are persisted

#### Scenario: Typed command results

- **WHEN** a command that returns a typed result completes successfully
- **THEN** the transaction is committed and the typed value is returned unchanged

### Requirement: One physical transaction per request

Only one physical transaction SHALL exist at a time for a given unit of work. Beginning a transaction while one is already active SHALL return a nested handle that does not commit or roll back the physical transaction. Rolling back a nested handle SHALL mark the outermost transaction rollback-only; committing a rollback-only outermost transaction SHALL fail and roll back instead.

#### Scenario: Nested begin does not open a second physical transaction

- **WHEN** a transaction is begun while another is already active
- **THEN** no second physical transaction is opened
- **AND** the returned handle's commit has no effect on the outermost transaction

#### Scenario: Command dispatched from inside an active transaction

- **WHEN** a command is handled while a transaction is already active
- **THEN** the command executes within the existing transaction
- **AND** that transaction is neither committed nor rolled back on the command's behalf

#### Scenario: Nested rollback poisons the outermost transaction

- **WHEN** a nested transaction handle is rolled back
- **AND** the outermost transaction is subsequently committed
- **THEN** the commit fails with an error
- **AND** the transaction is rolled back so that no writes are persisted

### Requirement: Queries do not open transactions

Query handling SHALL NOT open a transaction. A query that needs a multi-statement consistent read SHALL declare that explicitly at the call site.

#### Scenario: Query executes without a transaction

- **WHEN** a query is handled
- **THEN** no transaction is opened for it

### Requirement: Commit and rollback are not cancellable

Committing or rolling back a transaction SHALL ignore the request's cancellation token, because cancelling mid-commit would leave the transaction in an indeterminate state.

#### Scenario: Cancellation requested before commit

- **WHEN** the request's cancellation token is already cancelled and the command produced a successful result
- **THEN** the commit still runs to completion and the writes are persisted

#### Scenario: Cancellation requested before rollback

- **WHEN** the request's cancellation token is already cancelled and the command failed
- **THEN** the rollback still runs to completion

### Requirement: Domain event handler writes join the command transaction

Database writes performed by domain event handlers dispatched during a command SHALL be part of that command's transaction, and SHALL be reverted when the transaction is rolled back. The transactional guarantee — and its limit, that non-database side effects such as email or outbound HTTP are not covered — SHALL be documented on the domain event handler contract.

#### Scenario: Domain event handler write is committed with the command

- **WHEN** a command succeeds and a domain event handler wrote to the database during its dispatch
- **THEN** both the command's writes and the handler's writes are persisted

#### Scenario: Domain event handler write is reverted on rollback

- **WHEN** a domain event handler writes to the database and the command's transaction is subsequently rolled back
- **THEN** the handler's writes are not persisted

### Requirement: Configurable isolation level

The isolation level used for command transactions SHALL be configurable, defaulting to read-committed.

#### Scenario: Default isolation level

- **WHEN** no isolation level is configured
- **THEN** transactions are started with the read-committed isolation level

#### Scenario: Configured isolation level

- **WHEN** an isolation level is configured
- **THEN** transactions are started with the configured isolation level
