# Raftel.Demo.Application

Application layer of the "Pirates" example app. **Canonical reference for writing CQRS use cases** with `Raftel.Application`. Depends on `Raftel.Demo.Domain` and `Raftel.Application`.

## Structure

```
Pirates/
    CreatePirate/        CreatePirateCommand (record : ICommand, [RequiresPermission]) + Handler (sealed) + Validator (Validator<T>)
    GetPirateById/       GetPirateByIdQuery (record : IQuery<T>) + Handler + Response
    GetPirateByFilter/   Query + Handler + Response + PirateInfo (DTO)
    CreatePirateErrors.cs  use case errors
    PiratesPermissions.cs  feature permission constants
```

## What It Demonstrates (use as template)

- **Folder per use case**: each `<UseCase>/` groups Command/Query + Handler + Validator + Response.
- **Command**: `record CreatePirateCommand(...) : ICommand;` with `[RequiresPermission(PiratesPermissions.Management)]`.
- **Handler**: `sealed class ...Handler(IPirateRepository repository) : ICommandHandler<...>`, returns `Result`/`Result<T>`, no business logic (delegates to aggregate: `Pirate.Normal(...)`).
- **Validator**: `Validator<CreatePirateCommand>` with `EnsureThat(...)`; auto-discovered and registered via reflection.
- **Query**: read handler returning Response/DTO (DTOs can have public setters).

## Conventions

- ≤2 dependencies per handler; no EF Core (only repository interfaces).
- Permissions in `PiratesPermissions`; errors in `*Errors`. Keep consistent with `src/Raftel.Application/CLAUDE.md`.
