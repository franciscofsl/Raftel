## Purpose

Gives every domain-level failure a semantic category, and gives the API layer
a single, consistent way to translate that category into an HTTP response, so
clients can distinguish "not found" from "conflict" from "validation failed"
by status code instead of guessing from a flat 400.

## ADDED Requirements

### Requirement: Errors carry a semantic type
Every `Error` produced by the domain or application layer SHALL carry an
`ErrorType` classifying the failure as one of: `Failure` (unclassified),
`Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, or
`Unexpected`. An `Error` constructed without specifying a type SHALL default
to `Failure`.

#### Scenario: Constructing an error without a type
- **WHEN** code constructs an `Error` supplying only a code and a message
- **THEN** the resulting error's type is `Failure`

#### Scenario: Constructing an error via a typed factory
- **WHEN** code constructs an error via the `NotFound`, `Conflict`,
  `Validation`, `Unauthorized`, or `Forbidden` factory
- **THEN** the resulting error's type matches the factory used

### Requirement: Well-known error values are immutable
The system SHALL expose `Error.None` (absence of error) and `Error.NullValue`
(a null value was provided) as fixed, non-reassignable values. Code outside
the framework SHALL NOT be able to change what `Error.None` or
`Error.NullValue` represent for the rest of the process.

#### Scenario: Well-known error values keep their identity
- **WHEN** any code reads `Error.None` or `Error.NullValue` at any point during
  the application's lifetime
- **THEN** the value observed is always the one defined by the framework,
  regardless of what other code in the process has executed

### Requirement: Failed results are translated to HTTP problem responses
A failed `Result` returned by a command or query handler SHALL be translated
to an HTTP response whose status code is determined by the error's type:
`Validation` → 400, `NotFound` → 404, `Conflict` → 409, `Unauthorized` → 401,
`Forbidden` → 403, `Unexpected` → 500, and `Failure` → 400. The response body
SHALL be an RFC 7807 Problem Details document and SHALL include the original
`Error.Code` as a machine-readable extension field.

#### Scenario: Not-found business failure maps to 404
- **WHEN** a command or query handler returns a failed `Result` whose error
  type is `NotFound`
- **THEN** the HTTP response status is 404
- **AND** the response body is a Problem Details document containing the
  error's code

#### Scenario: Conflict business failure maps to 409
- **WHEN** a command handler returns a failed `Result` whose error type is
  `Conflict`
- **THEN** the HTTP response status is 409
- **AND** the response body is a Problem Details document containing the
  error's code

#### Scenario: Validation failure maps to 400
- **WHEN** a command or query handler returns a failed `Result` whose error
  type is `Validation`
- **THEN** the HTTP response status is 400
- **AND** the response body is a Problem Details document containing the
  error's code

#### Scenario: Forbidden failure maps to 403
- **WHEN** a command or query handler returns a failed `Result` whose error
  type is `Forbidden`
- **THEN** the HTTP response status is 403

#### Scenario: Unclassified failure defaults to 400
- **WHEN** a command or query handler returns a failed `Result` whose error
  type is `Failure` (unclassified)
- **THEN** the HTTP response status is 400

### Requirement: Successful command responses use operation-appropriate status codes
A command endpoint that completes successfully SHALL respond with a status
code appropriate to the outcome: `204 No Content` when the command produces no
result value; `201 Created` with a `Location` header when the command produces
a result value and the endpoint declares a created-resource route; `200 OK`
with the result value when the command produces a result value but declares no
created-resource route. Query endpoints are unaffected and continue to respond
`200 OK` with the query result on success.

#### Scenario: Command without a result value succeeds
- **WHEN** a command endpoint with no declared result type completes
  successfully
- **THEN** the HTTP response status is 204 and the body is empty

#### Scenario: Command with a result value and a declared created-resource route succeeds
- **WHEN** a command endpoint that declares a created-resource route completes
  successfully and produces a result value
- **THEN** the HTTP response status is 201
- **AND** the response includes a `Location` header pointing at the created
  resource

#### Scenario: Command with a result value and no declared created-resource route succeeds
- **WHEN** a command endpoint that does not declare a created-resource route
  completes successfully and produces a result value
- **THEN** the HTTP response status is 200 and the body contains the result
  value
