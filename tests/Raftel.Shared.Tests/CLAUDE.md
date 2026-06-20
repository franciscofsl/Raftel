# Raftel.Shared.Tests

**Unit** tests of `Raftel.Shared` cross-cutting utilities (`Enumerable`/`Queryable`/`String`/`Type` extensions, `DisposeAction`, marker attributes). Pure tests, no I/O.

## Stack

- **xUnit 2.9.3** + **Shouldly**.

## Conventions

- Case coverage: happy path, empty, null, and boundaries for each extension.
- `DisposeAction`: verify the action executes **on disposal** and only once.
- Fast and deterministic; one test file per utility family.
