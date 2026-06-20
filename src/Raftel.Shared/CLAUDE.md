# Raftel.Shared

Cross-cutting, low-level utilities reused by multiple layers. **No domain or infrastructure knowledge**: pure .NET generic helpers.

## Structure

```
Attributes/      MarkerAttribute (markers for discovery/reflection)
Extensions/      EnumerableExtensions, QueryableExtensions, StringExtensions, TypeExtensions
DisposeAction.cs IDisposable executing an Action on disposal (base of filter Disable() pattern)
```

## Patterns and Practices

- Everything here must be **domain-agnostic**: if a utility needs to know a Domain/Application type, it doesn't belong here.
- Extension methods grouped by the type they extend, one file per family.
- `DisposeAction` is the base of `using` patterns like temporary filter disabling in Infrastructure; keep its contract simple (execute once).
- No global mutable state; favor pure functions and side-effect-free extensions.
