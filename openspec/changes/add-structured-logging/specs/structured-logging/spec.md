## Purpose

Gives every HTTP request a single, structured, high-dimensionality log event (a "wide event" / canonical log line) describing everything relevant to that request's lifecycle — so a production incident can be diagnosed from one query instead of correlating scattered log lines, without ever leaking request payloads.

## ADDED Requirements

### Requirement: One wide event per request
The system SHALL emit exactly one structured log entry per HTTP request, capturing that request's outcome, instead of multiple log entries scattered across its lifecycle.

#### Scenario: Successful request
- **WHEN** an HTTP request is handled and completes without a failed `Result` or an unhandled exception
- **THEN** exactly one `Information` log entry is emitted for that request, including the request name and elapsed time

#### Scenario: Request fails with a business error
- **WHEN** a command or query handler returns a failed `Result`
- **THEN** exactly one `Warning` log entry is emitted for that request, including the request name, elapsed time, error code, and error message
- **THEN** no additional log entry is emitted for the same request

#### Scenario: Request throws an unhandled exception
- **WHEN** handling a request throws an exception, whether inside or outside the command/query pipeline
- **THEN** exactly one log entry is emitted for that request, including the request name (when known), elapsed time, and the exception
- **THEN** no additional log entry is emitted for the same request

### Requirement: Exception severity reflects its type
The system SHALL choose the wide event's log level for an unhandled exception based on its type: `Debug` for a validation exception, `Warning` for an authorization exception (including the required permission), and `Error` for any other exception.

#### Scenario: Validation exception
- **WHEN** a validation exception propagates while handling a request
- **THEN** the request's wide event is emitted at `Debug` level

#### Scenario: Authorization exception
- **WHEN** an authorization exception propagates while handling a request
- **THEN** the request's wide event is emitted at `Warning` level and includes the required permission

#### Scenario: Any other exception
- **WHEN** any other exception propagates while handling a request
- **THEN** the request's wide event is emitted at `Error` level

### Requirement: Stack traces never reach the HTTP response
The system SHALL NOT include an exception's stack trace in the HTTP response body, regardless of the wide event's log level.

#### Scenario: Any exception produces an error response
- **WHEN** an exception is translated into an HTTP error response
- **THEN** the response body does not contain the exception's stack trace

### Requirement: Explicit event enrichment
The system SHALL let request-handling code add named fields to the current request's wide event; every field added this way SHALL appear in that request's single emitted log entry.

#### Scenario: A handler enriches the event
- **WHEN** a command or query handler adds a field to the current request's event
- **THEN** the emitted log entry for that request includes that field

#### Scenario: Multiple components enrich the same event
- **WHEN** more than one component (e.g. authentication context and a handler) adds fields to the current request's event
- **THEN** all of those fields appear together in the single emitted log entry for that request

### Requirement: No request payload in logs by default
The system SHALL NOT automatically include the content (property values) of a command or query request in its wide event; only explicitly enriched fields and framework-owned metadata (request name, timing, outcome) are included.

#### Scenario: Request carries sensitive data
- **WHEN** a command containing a password, token, or other sensitive property is handled and no field is explicitly added for it
- **THEN** the emitted log entry for that request does not contain that property's value

### Requirement: Sensitive field names are refused
The system SHALL refuse to include a field in the wide event when its name matches a documented deny-list of sensitive names (e.g. `password`, `token`, `secret`, `apikey`, `authorization`), even when explicitly set by request-handling code.

#### Scenario: Code attempts to enrich with a denied field name
- **WHEN** request-handling code adds a field whose name matches the deny-list
- **THEN** that field does not appear in the emitted log entry for that request
