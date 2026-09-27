## Purpose

Guarantees that a request rejected by the command/query middleware pipeline — for a validation failure or a missing permission — is always reported to the caller as a failed `Result`, with the same HTTP contract as any other business-rule failure, never as a thrown exception that bypasses the pipeline.

## ADDED Requirements

### Requirement: Validation failures produce a failed Result
When one or more registered `Validator<TRequest>` instances report errors for an incoming command or query, the pipeline SHALL short-circuit and return a failed `Result` (or `Result<T>`) carrying a single aggregated validation error, without invoking the next handler in the chain and without throwing an exception.

#### Scenario: Single invalid field
- **WHEN** a command fails one validation rule (e.g. an empty required field)
- **THEN** the pipeline returns a failed `Result` whose error has `ErrorType.Validation`, and the handler is never invoked

#### Scenario: Multiple invalid fields
- **WHEN** a command fails validation rules on more than one field
- **THEN** the pipeline returns a single failed `Result` whose error aggregates every failed rule without dropping any of them, and the handler is never invoked

#### Scenario: Valid request
- **WHEN** a command or query passes every registered validation rule
- **THEN** the pipeline invokes the next handler and returns its result unchanged

### Requirement: Permission checks produce a failed Result
When the current caller is missing one or more permissions required by `[RequiresPermission]` on the request type, the pipeline SHALL short-circuit and return a failed `Result` with `ErrorType.Forbidden`, without invoking the next handler and without throwing an exception.

#### Scenario: Missing required permission
- **WHEN** a request decorated with `[RequiresPermission]` is dispatched by a caller lacking that permission
- **THEN** the pipeline returns a failed `Result` whose error has `ErrorType.Forbidden`, and the handler is never invoked

#### Scenario: Caller has every required permission
- **WHEN** a request decorated with `[RequiresPermission]` is dispatched by a caller holding all required permissions
- **THEN** the pipeline invokes the next handler and returns its result unchanged

#### Scenario: Request has no permission requirement
- **WHEN** a request carries no `[RequiresPermission]` attribute
- **THEN** the pipeline invokes the next handler without performing any permission check

### Requirement: Missing-permission detail is configurable and safe by default
The error message for a `Forbidden` failure SHALL only enumerate the specific missing permissions when explicitly enabled by configuration; by default it SHALL NOT reveal which permissions were required or missing.

#### Scenario: Default configuration in a Release-style deployment
- **WHEN** a permission check fails and permission-detail disclosure is not explicitly enabled
- **THEN** the resulting error message does not list the required or missing permission names

#### Scenario: Permission-detail disclosure explicitly enabled
- **WHEN** a permission check fails and permission-detail disclosure is explicitly enabled
- **THEN** the resulting error message lists the specific missing permissions

### Requirement: HTTP status codes are unchanged
The HTTP status code produced for a validation or permission failure SHALL be the same as before this capability existed: 400 for validation failures, 403 for authorization failures.

#### Scenario: Validation failure over HTTP
- **WHEN** a request that fails validation is submitted over HTTP
- **THEN** the response has status 400

#### Scenario: Authorization failure over HTTP
- **WHEN** a request from a caller missing a required permission is submitted over HTTP
- **THEN** the response has status 403

### Requirement: Validation failure body reports errors per field
The `ProblemDetails` body for a validation failure SHALL include an `errors` extension mapping each field name to the array of error messages for that field, replacing the prior flat array-of-`Error` shape.

#### Scenario: Single invalid field over HTTP
- **WHEN** a request fails validation on exactly one field
- **THEN** the response body's `errors` object has one entry keyed by that field's name, containing its error message(s)

#### Scenario: Multiple invalid fields over HTTP
- **WHEN** a request fails validation on more than one field
- **THEN** the response body's `errors` object has one entry per distinct failed field

### Requirement: Authorization failure body is unchanged
The `ProblemDetails` body for an authorization failure SHALL have the same shape as before this capability existed: no permission-revealing detail by default.

#### Scenario: Authorization failure body over HTTP
- **WHEN** a request from a caller missing a required permission is submitted over HTTP
- **THEN**, unless permission-detail disclosure is explicitly enabled, the response body does not enumerate the missing permissions

### Requirement: No framework middleware throws for business, validation, or authorization failure
No type in the command/query middleware pipeline SHALL throw an exception to signal a validation or authorization failure; such failures SHALL be communicated exclusively through a failed `Result`.

#### Scenario: Static check of the middleware pipeline
- **WHEN** the middleware pipeline is inspected for validation- or authorization-failure signaling
- **THEN** no middleware type throws an exception for either case
