# Raftel.Domain.Tests

**Unit** tests of the domain (`Raftel.Domain` and demo aggregates). No I/O, no containers: validate business rules, aggregate invariants, Value Objects, Specifications, and Result pattern.

## Stack

- **xUnit 2.9.3** (`[Fact]` / `[Theory]`) + **Shouldly** for assertions (`result.ShouldBe(...)`, `ShouldBeTrue()`).
- **NSubstitute** for test doubles when needed (rare in pure domain).

## Conventions

- TDD: test before implementation.
- Test name describing behavior: `Pirate_Should_BecomeKing_When_FoundOnePiece`.
- Explicit **Arrange / Act / Assert** structure.
- Test **behavior, not internal state**: use aggregate factory methods and business methods, check the `Result`/observable effect, not private fields.
- One test file per tested type, mirroring feature folder structure in Domain.
