# Raftel.Demo.Domain

Domain of the **"Pirates" example app** (One Piece) documenting how to use the framework. It's the **canonical reference** for modeling a domain with `Raftel.Domain`. Depends only on `Raftel.Domain`.

## Structure

```
Common/ValueObjects/   Name (shared VO)
Pirates/               Pirate aggregate (example root)
    Pirate.cs              AggregateRoot<PirateId>: private ctor, Normal()/Special() factories, business methods (FoundOnePiece, EatFruit)
    PirateErrors.cs        strongly-typed errors
    PirateValidator.cs     domain validation
    IPirateRepository.cs   interface (impl. in Demo.Infrastructure)
    ValueObjects/          PirateId (TypedGuidId), Bounty (readonly record struct with implicit conversion)
    DevilFruits/           entity/collection owned by aggregate + KnownDevilFruits, DevilFruitKind
    Specifications/        examples of combinable Specification<Pirate> (BountyOver, IsKing, And/Or)
    BlackBeardCrew.cs / MugiwaraCrew.cs   example data
Ships/                 Ship aggregate (simpler second example)
```

## What It Demonstrates (use as template)

- **Aggregate**: `Pirate : AggregateRoot<PirateId>` with private constructor, **factory methods** (`Pirate.Normal`, `Pirate.Special`) and business logic in methods (`EatFruit` returns `Result`). No public business setters.
- **Value Objects**: `Bounty` as `readonly record struct` with ctor validation and implicit operators; `PirateId` as `record : TypedGuidId` with `New()`.
- **First-class collections**: `DevilFruitCollection` encapsulates the fruit list; aggregate doesn't expose raw `List<>`.
- **Specifications**: reusable, combinable business filters translatable to SQL.
- **Errors**: `PirateErrors` as `static class` of `Error`.

## Conventions

- Strict Object Calisthenics and DDD (it's the framework showcase): no `else`, wrapped primitives, business names. Keep consistent with `src/Raftel.Domain/CLAUDE.md`.
