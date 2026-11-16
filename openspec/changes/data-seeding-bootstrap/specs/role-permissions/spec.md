# Spec Delta

## Purpose

Guarantees that the permissions granted to a role are stored with the role and are available whenever the role is loaded, so permission-based authorization works from the database rather than only within a single in-memory object graph.

## ADDED Requirements

### Requirement: Role permissions are persisted
The system SHALL persist the permissions granted to a role together with the role, and SHALL return exactly those permissions whenever the role is loaded afterwards.

#### Scenario: New role with permissions
- **WHEN** a role is created with permissions, saved, and loaded again in a new unit of work
- **THEN** the loaded role holds exactly the permissions it was saved with

#### Scenario: Permission added to an existing role
- **WHEN** a permission is added to a previously saved role and changes are saved
- **THEN** a later load of the role holds the earlier permissions and the added one

#### Scenario: Permission removed from a role
- **WHEN** a permission is removed from a previously saved role and changes are saved
- **THEN** a later load of the role no longer holds that permission

#### Scenario: Permission granted again after removal
- **WHEN** a permission is removed from a saved role, changes are saved, and the same permission is granted to the role again and saved
- **THEN** a later load of the role holds that permission

#### Scenario: Same permission name on two roles
- **WHEN** two different roles are granted a permission with the same name
- **THEN** both roles hold it independently

### Requirement: Role permissions reach the issued claims
The system SHALL include, in the permission claims of an issued access token, every permission held by the roles of the authenticated user as stored in the database.

#### Scenario: User with a stored role
- **WHEN** a user whose role was saved with permissions logs in
- **THEN** the issued token carries one permission claim per permission of that role
