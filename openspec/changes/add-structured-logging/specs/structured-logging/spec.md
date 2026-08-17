## Purpose

Gives every command and query execution a consistent, structured, and safe log trail — so a production incident (a failed command, an unhandled exception) can be diagnosed from logs alone, without ever leaking request payloads.

## ADDED Requirements

### Requirement: Request lifecycle logging
The system SHALL log the start and the outcome of every command/query request handled by the pipeline, using structured message templates with named placeholders (not string interpolation).

#### Scenario: Successful request
- **WHEN** a command or query is handled and completes without a failed `Result` or a thrown exception
- **THEN** an `Information` log entry is written when handling starts and another `Information` log entry is written on completion, including the request type name and elapsed time

#### Scenario: Request fails with a business error
- **WHEN** a command or query handler returns a failed `Result`
- **THEN** a `Warning` log entry is written including the request type name, the error code, the error message, and elapsed time
- **THEN** no `Error` level entry is written for this outcome

#### Scenario: Request throws an unhandled exception
- **WHEN** a command or query handler throws an exception
- **THEN** an `Error` log entry is written including the request type name and the exception, and the exception is rethrown unchanged

### Requirement: No request payload in logs
The system SHALL NOT include the content (property values) of a command or query request in any log entry it writes for that request.

#### Scenario: Request carries sensitive data
- **WHEN** a command containing a password, token, or other sensitive property is handled
- **THEN** the log entries written for that request contain only the request type name, not any of its property values

### Requirement: Unhandled exception is logged before responding
The system SHALL log any exception caught by the global exception handler at `Error` level, including the HTTP method and path, before producing an error response.

#### Scenario: Unexpected exception reaches the exception handler
- **WHEN** an unhandled exception propagates to the global exception handling middleware
- **THEN** an `Error` log entry is written with the exception and the request method/path
- **THEN** the HTTP response body does not contain the exception's stack trace

#### Scenario: Validation exception reaches the exception handler
- **WHEN** a validation exception propagates to the global exception handling middleware
- **THEN** a `Debug` log entry is written instead of `Error`

#### Scenario: Authorization exception reaches the exception handler
- **WHEN** an unauthorized-access exception propagates to the global exception handling middleware
- **THEN** a `Warning` log entry is written including the required permission
