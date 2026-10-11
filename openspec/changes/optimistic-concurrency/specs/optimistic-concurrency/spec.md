## Purpose

Prevents silent lost updates: when two callers change the same aggregate from the same starting state, the second write is rejected and reported instead of overwriting the first. Covers detection at the persistence boundary and the version exchange that lets HTTP clients take part in it.

## ADDED Requirements

### Requirement: Optimistic concurrency is opt-in per entity

An entity SHALL be protected against conflicting concurrent writes only when it has been explicitly declared as version-tracked. Entities that are not declared SHALL keep last-write-wins behaviour.

#### Scenario: Version-tracked entity rejects a stale write

- **WHEN** two units of work load the same version-tracked entity, the first saves a change, and the second then saves a change
- **THEN** the second save is rejected as a concurrency conflict
- **AND** the first unit of work's change remains persisted unmodified

#### Scenario: Untracked entity keeps last-write-wins

- **WHEN** two units of work load the same entity that is not version-tracked, and both save a change
- **THEN** both saves succeed
- **AND** the second save's values are the ones persisted

#### Scenario: Non-conflicting sequential writes succeed

- **WHEN** a version-tracked entity is loaded, changed and saved, then loaded again, changed and saved
- **THEN** both saves succeed

### Requirement: The version is maintained by the system

The version of a version-tracked entity SHALL change on every successful write to that entity without any action by domain or application code.

#### Scenario: Version changes after an update

- **WHEN** a version-tracked entity is updated and saved
- **THEN** the version read afterwards differs from the version read before the update

#### Scenario: Domain code does not manage the version

- **WHEN** a business operation changes a version-tracked aggregate
- **THEN** the conflict protection applies without the aggregate exposing or incrementing a version

### Requirement: Behaviour is the same on every supported database provider

Conflict detection SHALL behave identically on SQL Server and PostgreSQL.

#### Scenario: Stale write on SQL Server

- **WHEN** the stale-write scenario runs against SQL Server
- **THEN** the second save is rejected as a concurrency conflict

#### Scenario: Stale write on PostgreSQL

- **WHEN** the stale-write scenario runs against PostgreSQL
- **THEN** the second save is rejected as a concurrency conflict

### Requirement: Framework aggregates are version-tracked

The framework's user, tenant and role aggregates SHALL be version-tracked.

#### Scenario: Concurrent edits of the same user

- **WHEN** two units of work load the same user and both save a change
- **THEN** the second save is rejected as a concurrency conflict

#### Scenario: Concurrent edits of the same tenant or role

- **WHEN** two units of work load the same tenant, or the same role, and both save a change
- **THEN** the second save is rejected as a concurrency conflict

### Requirement: A conflict is reported to the application layer without persistence-framework types

A concurrency conflict raised while saving SHALL be surfaced as a framework-owned conflict signal that names the conflicting entity types and carries no persistence-framework type in its public surface.

#### Scenario: Conflict surfaces as a framework signal

- **WHEN** a save is rejected as a concurrency conflict
- **THEN** the caller observes the framework's concurrency conflict signal
- **AND** the signal identifies the type of each conflicting entity

#### Scenario: Other persistence failures are not reclassified

- **WHEN** a save fails for a reason other than a concurrency conflict
- **THEN** the original failure propagates unchanged

### Requirement: A conflicting command returns a conflict result

When the concurrency pipeline step is registered, a command whose save is rejected as a concurrency conflict SHALL complete with a failed result of type conflict and code `Concurrency.Conflict`, and SHALL NOT propagate an exception to the caller.

#### Scenario: Conflict becomes a failed result

- **WHEN** a command's save is rejected as a concurrency conflict
- **THEN** the command returns a failed result whose error type is conflict and whose code is `Concurrency.Conflict`

#### Scenario: Typed command

- **WHEN** a command that returns a value has its save rejected as a concurrency conflict
- **THEN** it returns a failed typed result with the same error

#### Scenario: Successful command is untouched

- **WHEN** a command completes without a concurrency conflict
- **THEN** its result is returned unchanged

#### Scenario: Other exceptions propagate

- **WHEN** a command throws an exception that is not a concurrency conflict
- **THEN** that exception propagates unchanged

### Requirement: A conflicting command persists nothing

A command that ends in a concurrency conflict SHALL leave no writes from that command persisted when it runs inside the command transaction boundary.

#### Scenario: Conflict rolls the command back

- **WHEN** a command writes one aggregate successfully and its save of a second, version-tracked aggregate is rejected as a concurrency conflict
- **THEN** no write made by that command is persisted

### Requirement: A conflict is answered with HTTP 409

A command endpoint whose command ends in a concurrency conflict SHALL respond `409 Conflict` with a Problem Details body carrying the code `Concurrency.Conflict`, unless the request carried an expected version (see the `If-Match` requirement).

#### Scenario: Conflict without If-Match

- **WHEN** a request without an `If-Match` header reaches a command endpoint and the command ends in a concurrency conflict
- **THEN** the response status is 409
- **AND** the body is Problem Details with `code` equal to `Concurrency.Conflict`

### Requirement: The version is excluded from audit logs

The version of a version-tracked entity SHALL NOT appear as a property change in audit logs, and a write that changes nothing but the version SHALL NOT produce an entity change entry.

#### Scenario: Update of a version-tracked entity

- **WHEN** an audited, version-tracked entity is updated
- **THEN** the audit log lists the changed business properties
- **AND** it contains no property change for the version

#### Scenario: Creation of a version-tracked entity

- **WHEN** an audited, version-tracked entity is created
- **THEN** the audit log's property list for that entity does not include the version

### Requirement: Version tracking coexists with soft delete and tenant filtering

Version tracking SHALL NOT alter soft-delete or tenant-isolation behaviour, and both SHALL remain subject to conflict detection.

#### Scenario: Soft-deleting a stale entity

- **WHEN** two units of work load the same version-tracked, soft-deletable entity, the first updates it and the second soft-deletes it
- **THEN** the second save is rejected as a concurrency conflict

#### Scenario: Tenant filter still applies

- **WHEN** a version-tracked, tenant-scoped entity is queried under a different tenant
- **THEN** it is not returned

### Requirement: Versioned query results carry an ETag

When ETag support is enabled, a query endpoint whose result exposes a version SHALL return that version as a strong `ETag` response header. When ETag support is disabled, or the result exposes no version, no `ETag` header SHALL be sent.

#### Scenario: ETag on a versioned result

- **WHEN** ETag support is enabled and a query endpoint returns a result that exposes a version
- **THEN** the response includes an `ETag` header containing that version in quoted form

#### Scenario: ETag changes after a write

- **WHEN** a resource is read, then modified, then read again
- **THEN** the second response's `ETag` differs from the first

#### Scenario: ETag support disabled

- **WHEN** ETag support is disabled and a query endpoint returns a result that exposes a version
- **THEN** the response has no `ETag` header

#### Scenario: Result without a version

- **WHEN** ETag support is enabled and a query endpoint returns a result that exposes no version
- **THEN** the response has no `ETag` header

### Requirement: If-Match enforces the client's expected version

When ETag support is enabled, a command endpoint that receives an `If-Match` header SHALL apply the command only if the version-tracked aggregate it modifies is still at that version; otherwise it SHALL respond `412 Precondition Failed` and persist nothing. The comparison SHALL be atomic with the write.

#### Scenario: Matching version

- **WHEN** a client reads a resource, then sends a command for it with `If-Match` set to the `ETag` it received, and nothing changed in between
- **THEN** the command is applied and the endpoint returns its normal success response

#### Scenario: Stale version

- **WHEN** client A and client B read the same resource, A's command succeeds, and B then sends its command with `If-Match` set to the `ETag` B originally received
- **THEN** B's response status is 412
- **AND** the body is Problem Details with `code` equal to `Concurrency.Conflict`
- **AND** A's change remains persisted unmodified

#### Scenario: Unrecognisable version value

- **WHEN** a command request carries an `If-Match` entity tag that is well-formed but is not a version the system could have issued
- **THEN** the response status is 412

#### Scenario: Malformed If-Match header

- **WHEN** a command request carries an `If-Match` header that is not a single quoted entity tag or `*`
- **THEN** the response status is 400 with a Problem Details body
- **AND** the command is not dispatched

#### Scenario: Wildcard If-Match

- **WHEN** a command request carries `If-Match: *`
- **THEN** no specific version is enforced and the command runs as it would without the header

#### Scenario: ETag support disabled

- **WHEN** ETag support is disabled and a command request carries an `If-Match` header
- **THEN** the header is ignored and the command runs as it would without it

### Requirement: Requests without If-Match keep current behaviour by default

A command request without an `If-Match` header SHALL be processed without an expected version, unless the command is declared as requiring one.

#### Scenario: No If-Match on an ordinary command

- **WHEN** ETag support is enabled and a command not declared as requiring an expected version is requested without `If-Match`
- **THEN** the command runs and the last write wins across requests

### Requirement: Commands can require an expected version

A command SHALL be declarable as requiring an expected version. When ETag support is enabled, its endpoint SHALL respond `428 Precondition Required` to a request without `If-Match`, without dispatching the command.

#### Scenario: Missing If-Match on a command that requires it

- **WHEN** ETag support is enabled and a command declared as requiring an expected version is requested without `If-Match`
- **THEN** the response status is 428 with a Problem Details body
- **AND** the command is not dispatched

#### Scenario: Wildcard does not satisfy the requirement

- **WHEN** ETag support is enabled and a command declared as requiring an expected version is requested with `If-Match: *`
- **THEN** the response status is 428

#### Scenario: Declaration is inert when ETag support is disabled

- **WHEN** ETag support is disabled and a command declared as requiring an expected version is requested without `If-Match`
- **THEN** the command runs normally
