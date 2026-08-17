## Purpose

Lets every log line produced while handling an HTTP request be tied back to that single request, and lets a caller supply or discover the identifier used to do so.

## ADDED Requirements

### Requirement: Correlation ID resolution
The system SHALL resolve a correlation ID for every incoming HTTP request: reusing a valid `X-Correlation-Id` request header when present, or generating a new identifier when absent.

#### Scenario: Request includes a valid correlation header
- **WHEN** an HTTP request arrives with an `X-Correlation-Id` header containing only alphanumeric characters, hyphens, or underscores, up to 128 characters
- **THEN** that value is used as the correlation ID for the request

#### Scenario: Request has no correlation header
- **WHEN** an HTTP request arrives without an `X-Correlation-Id` header
- **THEN** a new correlation ID is generated for the request

### Requirement: Correlation header sanitization
The system SHALL reject or discard an incoming `X-Correlation-Id` value that does not conform to the allowed format, generating a new correlation ID instead, so that untrusted client input cannot be used to inject or forge log lines.

#### Scenario: Header exceeds the maximum length
- **WHEN** an HTTP request arrives with an `X-Correlation-Id` header longer than 128 characters
- **THEN** the header value is discarded and a new correlation ID is generated

#### Scenario: Header contains disallowed characters
- **WHEN** an HTTP request arrives with an `X-Correlation-Id` header containing characters other than alphanumerics, hyphens, or underscores (including line breaks or control characters)
- **THEN** the header value is discarded and a new correlation ID is generated

### Requirement: Correlation ID propagation
The system SHALL echo the resolved correlation ID back to the caller and make it available as a logging scope value for every log entry written while handling the request.

#### Scenario: Response includes the correlation ID
- **WHEN** an HTTP request has been assigned a correlation ID (supplied or generated)
- **THEN** the HTTP response includes an `X-Correlation-Id` header with that value

#### Scenario: Logs during the request carry the correlation ID
- **WHEN** any log entry is written while handling an HTTP request that has a resolved correlation ID
- **THEN** that log entry includes the correlation ID as structured context
