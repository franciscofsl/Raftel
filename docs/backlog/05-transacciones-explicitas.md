# 05 — Transacciones explícitas en `IUnitOfWork`

> Estado: **Pendiente**
> Ámbito: `Raftel.Application`, `Raftel.Infrastructure`
> Rompe API pública: no (aditivo, con cambio de comportamiento interno)
> Depende de: [02](02-propagacion-cancellationtoken.md), [04](04-logging-estructurado.md)

## 1. Motivación

`UnitOfWorkMiddleware` hace:

```csharp
var response = await next();
if (response.IsSuccess)
{
    await unitOfWork.CommitAsync();
}
return response;
```

Esto **no es una transacción**, es un `SaveChanges`. EF Core envuelve un
`SaveChanges` en su propia transacción implícita, así que el caso simple funciona.
Pero se rompe en cuanto:

- Un handler llama a `CommitAsync` a mitad (por necesitar el ID generado) y luego
  sigue mutando: dos transacciones independientes, atomicidad perdida.
- Un handler de evento de dominio escribe en la base (`DomainEventsDispatchInterceptor`
  despacha en `SavedChanges`, o sea **después** del commit): sus cambios quedan
  fuera de la transacción del comando.
- La auditoría (`AuditStore`) persiste por separado.

No existe rollback. Si el paso 2 de un comando falla tras un commit intermedio,
el sistema queda en estado inconsistente.

## 2. Diseño

### 2.1 `IUnitOfWork` extendido

```csharp
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    // nuevos
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    bool HasActiveTransaction { get; }
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

`ITransaction` vive en `Raftel.Application` (no expone tipos de EF Core). La
implementación envuelve `IDbContextTransaction`.

### 2.2 Transacciones anidadas

Regla: **una sola transacción física por petición**. Si ya hay una activa,
`BeginTransactionAsync` devuelve un envoltorio no-op (`NestedTransaction`) cuyo
`CommitAsync` no hace nada y cuyo `RollbackAsync` marca la transacción raíz como
"solo rollback". Esto evita el error clásico de commits parciales anidados.

```csharp
internal sealed class NestedTransaction(RaftelTransaction root) : ITransaction
{
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken ct = default) { root.MarkRollbackOnly(); return Task.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

Si la raíz está marcada como rollback-only, su `CommitAsync` lanza
`InvalidOperationException` y hace rollback.

### 2.3 `TransactionMiddleware`

Middleware de comando nuevo, colocado **por fuera** de `UnitOfWorkMiddleware`:

```
LoggingMiddleware
  └ ValidationMiddleware
      └ PermissionAuthorizationMiddleware
          └ TransactionMiddleware      ← abre/commitea/rollback
              └ UnitOfWorkMiddleware   ← SaveChanges
                  └ Handler
```

```csharp
public sealed class TransactionMiddleware<TRequest>(IUnitOfWork unitOfWork, ILogger<...> logger)
    : ICommandMiddleware<TRequest> where TRequest : ICommand
{
    public async Task<Result> HandleAsync(TRequest request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            return await next(cancellationToken);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await next(cancellationToken);

            if (result.IsFailure)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return result;
            }

            await transaction.CommitAsync(CancellationToken.None);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
```

Variante genérica `TransactionMiddleware<TRequest, TResult>` para `ICommand<T>`.

**Commit y rollback con `CancellationToken.None`**: cancelar durante el commit
deja la transacción en estado indeterminado (coherente con la decisión §2.5 de
[02](02-propagacion-cancellationtoken.md)).

### 2.4 Solo comandos

Las queries no abren transacción. Si alguna necesita lectura consistente
multi-tabla, lo declara explícitamente. Abrir transacción en toda query es coste
puro.

### 2.5 Interacción con eventos de dominio

Con `TransactionMiddleware` activo, `DomainEventsDispatchInterceptor` sigue
despachando en `SavedChanges` (dentro de la transacción, antes del commit real).
Los handlers de evento que escriben en base **sí** entran ahora en la
transacción del comando. Esto mejora la consistencia pero **no** resuelve los
side-effects externos (email, HTTP): eso es [07](07-outbox-domain-events.md).

Documentar explícitamente en el XML doc de `IDomainEventHandler` qué garantía
transaccional se ofrece.

### 2.6 Nivel de aislamiento

Por defecto el del proveedor (`READ COMMITTED`). Configurable:

```csharp
public sealed class TransactionOptions
{
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}
```

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/ITransaction.cs`
- `src/Raftel.Application/TransactionOptions.cs`
- `src/Raftel.Application/Middlewares/TransactionMiddleware.cs`
- `src/Raftel.Infrastructure/Data/RaftelTransaction.cs`
- `src/Raftel.Infrastructure/Data/NestedTransaction.cs`
- `tests/Raftel.Application.UnitTests/Middlewares/TransactionMiddlewareTests.cs`
- `tests/Raftel.Infrastructure.Tests/Data/TransactionTests.cs`
- `tests/Raftel.Application.IntegrationTests/TransactionRollbackTests.cs`

**Modificados**
- `src/Raftel.Application/IUnitOfWork.cs`
- `src/Raftel.Infrastructure/Data/RaftelDbContext.cs` — implementar los métodos nuevos
- `src/Raftel.Infrastructure/DependencyInjection.cs`
- `demo/**/Program.cs` — registrar el middleware

## 4. Plan de implementación

1. **(test)** `TransactionMiddlewareTests` con `IUnitOfWork` sustituido: éxito ⇒
   `CommitAsync` una vez, `RollbackAsync` cero; `Result` fallido ⇒ rollback;
   excepción ⇒ rollback y la excepción se relanza.
2. **(test)** Con `HasActiveTransaction == true`, no se abre una segunda.
3. Definir `ITransaction`, extender `IUnitOfWork`.
4. Implementar `RaftelTransaction` y `NestedTransaction` en `RaftelDbContext`.
5. Implementar `TransactionMiddleware` (ambas variantes).
6. **(test integración, Testcontainers)** Comando que escribe en dos agregados y
   falla en el segundo ⇒ **ninguna** fila persistida.
7. **(test integración)** Handler de evento de dominio que escribe en base: sus
   cambios se revierten si la transacción hace rollback.
8. Registrar en la demo, documentar el orden de middlewares.
9. **(test integración)** Rollback-only anidado impide el commit de la raíz.

## 5. Tests

- **Unit**: matriz éxito / fallo / excepción / anidada.
- **Integration**: atomicidad real contra SqlServer y PostgreSQL.
- **Integration**: interacción con auditoría y eventos de dominio.

## 6. Criterios de aceptación

- [ ] Un comando que falla a mitad no deja ninguna escritura parcial.
- [ ] No se abren transacciones anidadas físicas.
- [ ] Las queries no abren transacción.
- [ ] Commit y rollback no son cancelables.
- [ ] Documentada la garantía transaccional de los handlers de evento de dominio.

## 7. Alternativas descartadas

- **`TransactionScope` / `System.Transactions`**: escalada a transacción
  distribuida en escenarios multi-conexión, mal soporte multiplataforma y coste
  alto para lo que se necesita.
- **Transacción abierta siempre en el `DbContext` scoped**: bloquea recursos
  durante toda la petición, incluidas las queries.
- **Meter el `BeginTransaction` dentro de `UnitOfWorkMiddleware`**: mezcla dos
  responsabilidades (persistir vs. delimitar la unidad atómica) y complica
  reutilizar el UoW sin transacción.
