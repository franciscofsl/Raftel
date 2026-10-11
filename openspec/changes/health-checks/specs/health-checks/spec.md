## Purpose

Lets an orchestrator, load balancer, or operator find out whether an application instance is alive, whether it is ready to receive traffic, and — for authorized callers only — which dependency is degraded, without a failing dependency causing restart loops or leaking internal detail to anonymous callers.

## ADDED Requirements

### Requirement: Liveness probe independent of dependencies
The system SHALL expose a liveness endpoint at `/health/live` that reports whether the process itself is responsive, and SHALL NOT contact the database or any other external dependency while serving it.

#### Scenario: Process is responsive and dependencies are healthy
- **WHEN** a caller requests `/health/live` while the application is running normally
- **THEN** the response status code is 200

#### Scenario: Database is unreachable
- **WHEN** a caller requests `/health/live` while the database is unreachable
- **THEN** the response status code is 200
- **THEN** no database connection is attempted while serving the request

#### Scenario: Liveness is reachable without credentials
- **WHEN** an unauthenticated caller requests `/health/live`
- **THEN** the request is not rejected for lack of authentication

### Requirement: Readiness probe reflecting dependencies
The system SHALL expose a readiness endpoint at `/health/ready` that aggregates only the checks marked as readiness checks, returning 200 when the aggregate result is healthy or degraded, and 503 when it is unhealthy.

#### Scenario: All readiness checks pass
- **WHEN** a caller requests `/health/ready` and every readiness check reports healthy
- **THEN** the response status code is 200

#### Scenario: A readiness check is unhealthy
- **WHEN** a caller requests `/health/ready` and at least one readiness check reports unhealthy
- **THEN** the response status code is 503

#### Scenario: A readiness check is degraded
- **WHEN** a caller requests `/health/ready` and at least one readiness check reports degraded while none reports unhealthy
- **THEN** the response status code is 200
- **THEN** the aggregate status reported is degraded

#### Scenario: Readiness is reachable without credentials
- **WHEN** an unauthenticated caller requests `/health/ready`
- **THEN** the request is not rejected for lack of authentication

### Requirement: Detailed diagnostic endpoint
The system SHALL expose a diagnostic endpoint at `/health` that runs every registered check and reports each check's individual result.

#### Scenario: Authorized caller gets per-check detail
- **WHEN** an authorized caller requests `/health`
- **THEN** the response body reports the aggregate status and one entry per registered check, each with its own status

#### Scenario: Aggregate status maps to a status code
- **WHEN** an authorized caller requests `/health` and at least one check reports unhealthy
- **THEN** the response status code is 503

### Requirement: Detail endpoint requires authorization by default
The system SHALL require authorization for `/health` by default, and SHALL allow an operator to configure the authorization policy applied to it or to opt out of the requirement explicitly.

#### Scenario: Anonymous caller under default configuration
- **WHEN** an unauthenticated caller requests `/health` while the default configuration is in effect
- **THEN** the response status code is 401
- **THEN** the response body contains no check names, statuses, or descriptions

#### Scenario: Operator configures a policy
- **WHEN** an operator configures an authorization policy for the detail endpoint and a caller who does not satisfy that policy requests `/health`
- **THEN** the request is rejected and the response body contains no check detail

#### Scenario: Operator opts out of the requirement
- **WHEN** an operator explicitly disables the authorization requirement for the detail endpoint and an unauthenticated caller requests `/health`
- **THEN** the detailed response body is returned

### Requirement: Public probe responses expose status only
The system SHALL limit the response body of `/health/live` and `/health/ready` to aggregate status information, and SHALL NOT include per-check descriptions, exception messages, stack traces, connection strings, or any other configuration value in any health response body, at any endpoint.

#### Scenario: Readiness fails because of a database error
- **WHEN** a caller requests `/health/ready` while the database connection fails with an exception
- **THEN** the response body contains no exception message, no stack trace, and no connection string
- **THEN** the response body contains no per-check description

#### Scenario: Detailed endpoint reports a failing check
- **WHEN** an authorized caller requests `/health` while a check has failed with an exception
- **THEN** the response body reports that check's status and a description that does not contain the exception message, a stack trace, or a connection string

### Requirement: JSON health response format
The system SHALL serialize health responses as JSON containing the aggregate `status` and the total duration in milliseconds, and — for the detailed endpoint — an `entries` object keyed by check name, where each entry carries that check's `status`, its duration in milliseconds, and an optional non-sensitive `description`.

#### Scenario: Detailed response shape
- **WHEN** an authorized caller requests `/health`
- **THEN** the response content type is `application/json`
- **THEN** the body contains the aggregate status, the total duration in milliseconds, and an entry per check with that check's status and duration

#### Scenario: Public probe response shape
- **WHEN** a caller requests `/health/live` or `/health/ready`
- **THEN** the response content type is `application/json`
- **THEN** the body contains the aggregate status and no `entries` object

### Requirement: Database connectivity check
The system SHALL provide a readiness check that verifies the application can connect to its database, reporting healthy on a successful connection, degraded when the connection succeeds but takes longer than the configured slow threshold, and unhealthy when the connection fails or does not complete within the configured check timeout.

#### Scenario: Database reachable and fast
- **WHEN** the database connectivity check runs and the connection succeeds within the slow threshold
- **THEN** the check reports healthy

#### Scenario: Database reachable but slow
- **WHEN** the database connectivity check runs and the connection succeeds but takes longer than the configured slow threshold
- **THEN** the check reports degraded

#### Scenario: Database unreachable
- **WHEN** the database connectivity check runs against an unreachable database or an invalid connection string
- **THEN** the check reports unhealthy
- **THEN** no exception propagates out of the check

### Requirement: Pending migrations check
The system SHALL provide a readiness check that reports unhealthy while the database schema has migrations that have not been applied, so that traffic is not served against an outdated schema.

#### Scenario: Schema is up to date
- **WHEN** the pending migrations check runs and no migration is pending
- **THEN** the check reports healthy

#### Scenario: Migrations are pending
- **WHEN** the pending migrations check runs and at least one migration is pending
- **THEN** the check reports unhealthy

#### Scenario: Migration state cannot be determined
- **WHEN** the pending migrations check cannot reach the database to determine migration state
- **THEN** the check reports unhealthy
- **THEN** no exception propagates out of the check

### Requirement: Startup readiness gate
The system SHALL report the instance as not ready from process start until application startup work has completed, and SHALL report it ready afterwards, so no traffic is admitted during the window in which the process responds but startup is unfinished.

#### Scenario: Startup work still running
- **WHEN** a caller requests `/health/ready` before startup work has completed
- **THEN** the response status code is 503

#### Scenario: Startup work completed
- **WHEN** startup work has completed and a caller requests `/health/ready` while every other readiness check passes
- **THEN** the response status code is 200

#### Scenario: Startup work failed
- **WHEN** application startup work fails
- **THEN** the instance continues to report not ready

### Requirement: Tenant resolution check
The system SHALL provide an opt-in readiness check that verifies the tenant store responds, reporting healthy when it does and unhealthy when it does not, and SHALL NOT register the check unless the application opts in.

#### Scenario: Tenant store responds
- **WHEN** the tenant resolution check runs and the tenant store responds successfully
- **THEN** the check reports healthy

#### Scenario: Tenant store does not respond
- **WHEN** the tenant resolution check runs and the tenant store fails or times out
- **THEN** the check reports unhealthy
- **THEN** no exception propagates out of the check

#### Scenario: Check not opted in
- **WHEN** an application does not opt in to the tenant resolution check
- **THEN** no tenant entry appears in the detailed health response

### Requirement: Outbox backlog check
The system SHALL provide a readiness check that reports degraded when unprocessed outbox messages are older than the configured lag threshold, unhealthy when the number of dead-lettered messages exceeds the configured dead-letter threshold, and healthy otherwise; the check SHALL only be registered when an outbox is present in the application.

#### Scenario: Outbox keeping up
- **WHEN** the outbox check runs, no unprocessed message is older than the lag threshold, and dead letters do not exceed the dead-letter threshold
- **THEN** the check reports healthy

#### Scenario: Outbox lagging
- **WHEN** the outbox check runs and at least one unprocessed message is older than the configured lag threshold
- **THEN** the check reports degraded

#### Scenario: Dead letters above threshold
- **WHEN** the outbox check runs and the number of dead-lettered messages exceeds the configured dead-letter threshold
- **THEN** the check reports unhealthy

#### Scenario: No outbox present
- **WHEN** an application has no outbox
- **THEN** no outbox entry appears in the detailed health response

### Requirement: A hung check cannot hang the endpoint
The system SHALL bound each individual check by the configured check timeout, reporting that check as unhealthy when it exceeds the timeout, while the remaining checks still report their own results and the endpoint still responds.

#### Scenario: One check never completes
- **WHEN** a health endpoint is requested and one registered check does not complete within the configured check timeout
- **THEN** the endpoint returns a response
- **THEN** the timed-out check is reported as unhealthy
- **THEN** every other check reports its own result

### Requirement: Configurable health options
The system SHALL let an application configure the detail endpoint's authorization requirement and policy, the per-check timeout, the database slow threshold, the outbox lag threshold, and the dead-letter threshold, and SHALL apply documented defaults when they are not configured: authorization required for the detail endpoint, a 3-second check timeout, a 1-second database slow threshold, a 5-minute outbox lag threshold, and a dead-letter threshold of 10.

#### Scenario: Defaults applied
- **WHEN** an application registers health checks without configuring options
- **THEN** the documented default values are in effect

#### Scenario: Thresholds overridden
- **WHEN** an application configures a different database slow threshold and a caller requests the detail endpoint while a database connection takes longer than the configured value
- **THEN** the database check reports degraded according to the configured threshold
