# Spec Delta

## Purpose

Brings an application from an empty database to a usable state at startup — permissions, roles, an initial administrator and an optional default tenant — through ordered, idempotent seeders that consumers can extend, without ever shipping a default credential.

## ADDED Requirements

### Requirement: Seeders run in a deterministic order
The system SHALL execute every registered seeder in ascending order of its declared order value, breaking ties by seeder name, and SHALL reject startup seeding when two seeders declare the same name.

#### Scenario: Seeders declare different orders
- **WHEN** seeding runs with seeders declaring orders 300, 100 and 200
- **THEN** they execute in the sequence 100, 200, 300

#### Scenario: Two seeders share an order
- **WHEN** seeding runs with two seeders declaring the same order
- **THEN** they execute in ascending order of their names

#### Scenario: Two seeders share a name
- **WHEN** seeding runs with two seeders declaring the same name
- **THEN** seeding fails before any seeder executes
- **THEN** the failure names the duplicated seeder name

### Requirement: Consumer seeders are discovered automatically
The system SHALL discover seeders defined in the application assemblies a consumer registers with the framework and include them in seeding without any per-seeder registration.

#### Scenario: Consumer defines a seeder
- **WHEN** a consumer defines a seeder in a registered application assembly and seeding runs
- **THEN** that seeder executes at the position its order dictates relative to the built-in seeders

### Requirement: Each seeder is isolated in its own transaction
The system SHALL run each seeder in its own transaction, committing its changes when it reports success and discarding them when it reports failure or throws, so that a failing seeder never reverts seeders that already completed.

#### Scenario: A later seeder fails
- **WHEN** seeders at orders 100 and 200 succeed and the seeder at order 300 fails
- **THEN** the data written by seeders 100 and 200 remains persisted
- **THEN** nothing written by seeder 300 is persisted

#### Scenario: A seeder throws
- **WHEN** a seeder throws an exception instead of reporting a failure result
- **THEN** its changes are discarded
- **THEN** the seeder is treated as failed with the exception reported in the log

### Requirement: Failure handling is configurable
The system SHALL, when fail-fast is enabled (the default), stop executing further seeders after the first failure and abort application startup with an error identifying the failed seeder. When fail-fast is disabled, the system SHALL log the failure, continue with the remaining seeders, and let the application start.

#### Scenario: Failure with fail-fast enabled
- **WHEN** fail-fast is enabled and the seeder at order 200 fails while a seeder at order 300 is registered
- **THEN** the seeder at order 300 does not execute
- **THEN** application startup is aborted with an error naming the failed seeder

#### Scenario: Failure with fail-fast disabled
- **WHEN** fail-fast is disabled and the seeder at order 200 fails while a seeder at order 300 is registered
- **THEN** the seeder at order 300 executes
- **THEN** the application starts
- **THEN** the failure is recorded in the log

### Requirement: Seeding is idempotent
The system SHALL leave the database in the same state when seeding runs any number of times against unchanged code and configuration.

#### Scenario: Seeding runs twice
- **WHEN** seeding completes against an empty database and then runs again with no code or configuration change
- **THEN** the second run creates no additional tenants, permissions, roles, role permissions or users
- **THEN** the second run reports success

### Requirement: Seeder executions are recorded
The system SHALL record each successful seeder execution with the seeder name, the version it declared, the tenant it ran for (if any) and the time it was applied. A failed execution SHALL NOT be recorded as applied.

#### Scenario: A seeder succeeds
- **WHEN** a seeder completes successfully
- **THEN** a history record exists with its name, declared version and application time

#### Scenario: A seeder fails
- **WHEN** a seeder fails
- **THEN** no history record marks that execution as applied

### Requirement: Run-once seeders are not re-executed
The system SHALL skip a seeder declared as run-once when history shows it was already applied at its current or a higher version, and SHALL execute it again when its declared version is higher than the recorded one. Seeders not declared run-once SHALL execute on every run.

#### Scenario: Run-once seeder already applied
- **WHEN** seeding runs and a run-once seeder has a history record at its current version
- **THEN** the seeder does not execute

#### Scenario: Run-once seeder version is raised
- **WHEN** seeding runs and a run-once seeder declares a version higher than the one recorded in history
- **THEN** the seeder executes
- **THEN** history reflects the new version

#### Scenario: Reconciling seeder
- **WHEN** seeding runs and a seeder not declared run-once already has a history record
- **THEN** the seeder executes again

### Requirement: Permission catalog is reconciled with code
The system SHALL maintain a persisted catalog of permissions and, on every seeding run, synchronize it with the permission names declared as string constants in classes marked as permission sets — both the framework's and the consumer's. Permissions present in code and absent from the catalog SHALL be added. Existing catalog entries SHALL NOT be duplicated or altered.

#### Scenario: Empty database
- **WHEN** seeding runs against an empty database
- **THEN** the catalog contains one entry per permission constant declared in a marked permission set

#### Scenario: New permission added to code
- **WHEN** a new permission constant is added to a marked permission set and seeding runs against a database that was already seeded
- **THEN** the new permission is added to the catalog
- **THEN** the pre-existing catalog entries are unchanged

#### Scenario: Unmarked class is ignored
- **WHEN** a class declares string constants but is not marked as a permission set
- **THEN** its constants are not added to the catalog

### Requirement: Permissions removed from code are flagged, not deleted
The system SHALL flag as obsolete any catalog permission that is no longer declared in code, SHALL NOT delete it or remove it from roles holding it, and SHALL report each newly obsolete permission in the log. A flagged permission that reappears in code SHALL have the flag cleared.

#### Scenario: Permission disappears from code
- **WHEN** a permission constant is removed from code and seeding runs
- **THEN** the catalog entry still exists and is flagged obsolete
- **THEN** roles that held the permission still hold it
- **THEN** the log reports the permission as obsolete

#### Scenario: Permission reappears in code
- **WHEN** a permission flagged obsolete is declared in code again and seeding runs
- **THEN** the catalog entry is no longer flagged obsolete

### Requirement: Default roles are seeded and reconciled
The system SHALL create each configured default role that does not exist and grant it the configured permissions, and SHALL add configured permissions missing from a role that already exists without removing permissions the role holds beyond the configured ones. With no role configuration, the system SHALL seed a single administrator role holding every non-obsolete catalog permission. A configured permission that is not in the catalog SHALL cause the seeder to fail.

#### Scenario: Empty database with default configuration
- **WHEN** seeding runs against an empty database with no role configuration
- **THEN** an administrator role exists holding every non-obsolete catalog permission

#### Scenario: New permission reaches the administrator role
- **WHEN** a new permission is added to code and seeding runs against a database where the administrator role already exists
- **THEN** the administrator role holds the new permission

#### Scenario: Role was customized after seeding
- **WHEN** an existing default role holds a permission that is not in its configuration and seeding runs
- **THEN** the role still holds that permission

#### Scenario: Configured permission is unknown
- **WHEN** role configuration names a permission that is not in the catalog
- **THEN** the default-roles seeder fails with an error naming the unknown permission

### Requirement: Initial administrator is seeded
The system SHALL create an administrator user with the configured email and password and assign it the administrator role when no user with that email exists. When the user already exists, the system SHALL NOT change its password and SHALL ensure the administrator role is assigned.

#### Scenario: Empty database
- **WHEN** seeding runs against an empty database with administrator email and password configured
- **THEN** a user with that email exists and can authenticate with that password
- **THEN** the user holds the administrator role and, through it, every non-obsolete catalog permission

#### Scenario: Administrator already exists
- **WHEN** seeding runs and a user with the configured administrator email already exists
- **THEN** no second user is created
- **THEN** the existing user's password is unchanged

#### Scenario: Password violates the password policy
- **WHEN** the configured administrator password does not satisfy the identity password policy
- **THEN** the administrator seeder fails with an error describing the policy violation
- **THEN** no administrator user is created

### Requirement: Administrator credentials are never defaulted
The system SHALL NOT contain a default administrator password in code or in any configuration file shipped in the repository. Outside the Development environment, when the administrator password or email is not configured, the administrator seeder SHALL fail with a message stating which setting is missing and SHALL NOT create the user.

#### Scenario: Missing password in Production
- **WHEN** seeding runs in a Production environment with no administrator password configured
- **THEN** the administrator seeder fails with a message naming the missing setting
- **THEN** no administrator user is created
- **THEN** with fail-fast enabled, application startup is aborted

#### Scenario: Missing email in Production
- **WHEN** seeding runs in a Production environment with no administrator email configured
- **THEN** the administrator seeder fails with a message naming the missing setting
- **THEN** no administrator user is created

### Requirement: Generated administrator passwords are development-only
The system SHALL generate a random administrator password only in the Development environment and only when creating the administrator, writing it to the log once and marked as development-only. A configured password SHALL never be written to the log.

#### Scenario: Missing password in Development
- **WHEN** seeding runs in a Development environment with no administrator password configured and no administrator user exists
- **THEN** an administrator user is created with a randomly generated password
- **THEN** the generated password is written to the log exactly once, marked as development-only

#### Scenario: Subsequent Development start
- **WHEN** seeding runs again in a Development environment after the administrator was created with a generated password
- **THEN** no new password is generated or logged

#### Scenario: Configured password is not logged
- **WHEN** seeding creates the administrator with a configured password
- **THEN** the password does not appear in any log output

### Requirement: Default tenant is seeded when configured
The system SHALL create the configured default tenant when no tenant with its code exists, and SHALL do nothing when no default tenant is configured or the tenant already exists.

#### Scenario: Default tenant configured
- **WHEN** seeding runs with a default tenant configured and no tenant with that code exists
- **THEN** a tenant with the configured name and code exists

#### Scenario: Default tenant already exists
- **WHEN** seeding runs with a default tenant configured and a tenant with that code already exists
- **THEN** no additional tenant is created

#### Scenario: No default tenant configured
- **WHEN** seeding runs with no default tenant configured
- **THEN** no tenant is created by seeding

### Requirement: Tenant seeders run once per tenant in that tenant's context
The system SHALL, after the global seeders, execute every tenant seeder once for each existing tenant with the current tenant set to that tenant, each execution in its own transaction and recorded in history per tenant.

#### Scenario: Multiple tenants exist
- **WHEN** seeding runs with one tenant seeder registered and two tenants existing
- **THEN** the tenant seeder executes twice, once per tenant
- **THEN** during each execution the current tenant is the tenant being seeded
- **THEN** data written by each execution belongs to the corresponding tenant

#### Scenario: Tenant seeder fails for one tenant
- **WHEN** a tenant seeder succeeds for the first tenant and fails for the second
- **THEN** the data written for the first tenant remains persisted

### Requirement: A newly created tenant is seeded
The system SHALL execute every tenant seeder for a tenant when that tenant is created through the application, in the new tenant's context. When tenant creation runs inside a transaction and a tenant seeder fails, the tenant creation SHALL fail and nothing SHALL be persisted.

#### Scenario: Tenant created at runtime
- **WHEN** a tenant is created through the application while a tenant seeder is registered
- **THEN** the tenant seeder executes for the new tenant with the current tenant set to it

#### Scenario: Tenant seeder fails during creation
- **WHEN** a tenant is created inside a transaction and a tenant seeder fails for it
- **THEN** the creation request fails
- **THEN** neither the tenant nor any seeded data is persisted

### Requirement: Migrations are applied at startup only when enabled
The system SHALL apply pending database migrations before seeding only when applying migrations on startup is enabled, which it is not by default. When it is disabled and migrations are pending, seeding SHALL fail with a message stating that migrations are pending, without executing any seeder.

#### Scenario: Migration on startup enabled
- **WHEN** the application starts with migration on startup enabled and migrations pending
- **THEN** the migrations are applied before any seeder executes

#### Scenario: Migration on startup disabled with pending migrations
- **WHEN** the application starts with migration on startup disabled and migrations pending
- **THEN** no migration is applied
- **THEN** no seeder executes
- **THEN** seeding fails with a message stating that migrations are pending

### Requirement: Startup seeding can be disabled
The system SHALL skip all seeders at startup when running seeders on startup is disabled, while still applying migrations if that is enabled.

#### Scenario: Seeders disabled
- **WHEN** the application starts with running seeders on startup disabled
- **THEN** no seeder executes
- **THEN** the application starts and reports ready

### Requirement: Readiness reflects seeding
The system SHALL report the instance as not ready on `/health/ready` while startup seeding is in progress and after it has failed with fail-fast enabled, ready once it completes successfully, and degraded when it completes with failures while fail-fast is disabled. An application that never requests startup seeding SHALL not be held not ready by this check.

#### Scenario: Seeding in progress
- **WHEN** a caller requests `/health/ready` while startup seeding is still running
- **THEN** the response status is 503

#### Scenario: Seeding completed
- **WHEN** startup seeding has completed successfully and every other readiness check passes
- **THEN** `/health/ready` responds 200

#### Scenario: Seeding completed with failures and fail-fast disabled
- **WHEN** startup seeding finished with at least one failed seeder while fail-fast is disabled and every other readiness check passes
- **THEN** `/health/ready` responds 200 with aggregate status degraded

#### Scenario: Seeding never requested
- **WHEN** the application starts without requesting startup seeding and every other readiness check passes
- **THEN** `/health/ready` responds 200
