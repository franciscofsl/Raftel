# 08 — Concurrencia optimista

> Estado: **Pendiente**
> Ámbito: `Raftel.Domain`, `Raftel.Infrastructure`, `Raftel.Api.Server`
> Rompe API pública: no (opt-in por entidad)
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [05](05-transacciones-explicitas.md)

## 1. Motivación

No existe ningún token de concurrencia en el modelo. `Entity<TId>` tiene solo
`Id`, y ninguna configuración EF declara `IsRowVersion()` ni `IsConcurrencyToken()`.

Escenario de pérdida de actualización (lost update), hoy silencioso:

```
t0  Usuario A lee Tenant{Name="Acme", Plan="Free"}
t1  Usuario B lee Tenant{Name="Acme", Plan="Free"}
t2  A ejecuta EditTenant{Plan="Pro"}       → guardado
t3  B ejecuta EditTenant{Name="Acme Corp"} → guardado, sobrescribe Plan="Free"
```

El cambio de A se pierde sin aviso. En un framework con `EditUser`,
`AssignRoleToUser` y multitenancy esto es un problema real, no teórico.

## 2. Diseño

### 2.1 `IHasVersion`

Opt-in por agregado, no obligatorio: no todo agregado lo necesita y la columna
tiene coste.

```csharp
// Raftel.Domain/Abstractions/IHasVersion.cs
public interface IHasVersion
{
    byte[] Version { get; }
}
```

Alternativa considerada para portabilidad: `uint Version` incremental. Ver §7.
Se elige `byte[]` con mapeo por proveedor:

| Proveedor | Mapeo |
|---|---|
| SqlServer | `rowversion` nativo (`IsRowVersion()`) |
| PostgreSQL | columna de sistema `xmin` (`UseXminAsConcurrencyToken()`) |

Ambos los gestiona el motor: no hay que incrementar nada a mano.

### 2.2 Configuración

Extensión simétrica a la ya existente `HasSoftDelete()`:

```csharp
// Raftel.Infrastructure/Data/IEntityTypeConfigurationExtensions.cs
public static void HasOptimisticConcurrency<TEntity>(this EntityTypeBuilder<TEntity> builder, DatabaseProvider provider)
    where TEntity : class
{
    switch (provider)
    {
        case DatabaseProvider.SqlServer:
            builder.Property<byte[]>(ShadowPropertyNames.Version).IsRowVersion();
            break;
        case DatabaseProvider.PostgreSql:
            builder.UseXminAsConcurrencyToken();
            break;
    }
}
```

Se usa **shadow property**, igual que `IsDeleted`, para no contaminar el
agregado con una preocupación de persistencia. `IHasVersion` queda como marcador
opcional para los agregados que sí quieran exponer la versión al cliente
(ver §2.4).

`ShadowPropertyNames` gana `public const string Version = nameof(Version);`.

### 2.3 Traducir el fallo a `Result`

`DbUpdateConcurrencyException` no debe llegar a la capa de aplicación como
excepción de EF. Se captura en `RaftelDbContext.SaveChangesAsync` y se convierte:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    try
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
        throw new ConcurrencyConflictException(ExtractEntityNames(exception), exception);
    }
}
```

Y en el pipeline, un `ConcurrencyMiddleware` de comando lo convierte a
`Result.Failure(Error.Conflict("Concurrency.Conflict", ...))` ⇒ **HTTP 409**
gracias a [01](01-error-tipado-y-mapeo-http.md).

> Se mantiene una excepción interna (`ConcurrencyConflictException`) porque el
> fallo nace dentro de EF Core, fuera del alcance del patrón `Result`. La
> conversión ocurre en el borde del pipeline, no se propaga a los handlers.

### 2.4 ETags HTTP (opcional, misma feature)

Para agregados que implementen `IHasVersion`, los endpoints pueden exponer
concurrencia optimista de extremo a extremo:

- `GET` devuelve `ETag: "base64(version)"`.
- `PUT`/`PATCH` acepta `If-Match`; si no coincide ⇒ `412 Precondition Failed`.
- Sin `If-Match` ⇒ comportamiento actual (último gana), configurable a
  `428 Precondition Required`.

Implementado en `QueryEndpointMapper` / `CommandEndpointMapper`, activable con
`ConcurrencyOptions.EnableETags`.

### 2.5 Política de reintento

Para comandos idempotentes se ofrece `[RetryOnConcurrencyConflict(MaxAttempts = 3)]`.
El `ConcurrencyMiddleware` reejecuta el handler con estado recargado. **No** se
aplica por defecto: reintentar un comando no idempotente duplica efectos.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Domain/Abstractions/IHasVersion.cs`
- `src/Raftel.Application/Exceptions/ConcurrencyConflictException.cs`
- `src/Raftel.Application/Middlewares/ConcurrencyMiddleware.cs`
- `src/Raftel.Application/ConcurrencyOptions.cs`
- `src/Raftel.Application/Authorization/RetryOnConcurrencyConflictAttribute.cs`
- `tests/Raftel.Infrastructure.Tests/Data/ConcurrencyTests.cs`
- `tests/Raftel.Application.IntegrationTests/LostUpdateTests.cs`

**Modificados**
- `src/Raftel.Infrastructure/Data/ShadowPropertyNames.cs`
- `src/Raftel.Infrastructure/Data/IEntityTypeConfigurationExtensions.cs`
- `src/Raftel.Infrastructure/Data/RaftelDbContext.cs`
- `src/Raftel.Infrastructure/Data/Configuration/{User,Tenant,Role}Configuration.cs`
- `src/Raftel.Infrastructure/Data/Auditing/ChangeSnapshotExtractor.cs` — excluir
  `Version` de la auditoría (hoy ya excluye `IsDeleted`)
- `src/Raftel.Api.Server/AutoEndpoints/*` — ETags

## 4. Plan de implementación

1. **(test integración)** Reproducir el lost update **antes** de arreglarlo: dos
   `DbContext`, lectura simultánea, dos escrituras ⇒ el test demuestra la pérdida.
2. Añadir `ShadowPropertyNames.Version` y `HasOptimisticConcurrency`.
3. Aplicarlo a `User`, `Tenant`, `Role`.
4. **(test integración)** El mismo escenario ahora lanza
   `DbUpdateConcurrencyException` en la segunda escritura.
5. Traducir en `RaftelDbContext` a `ConcurrencyConflictException`.
6. **(test)** `ConcurrencyMiddleware` convierte a `Error.Conflict`.
7. Implementar el middleware.
8. **(test funcional)** El endpoint responde `409` con `ProblemDetails`.
9. **(test)** La auditoría no registra cambios en la columna `Version`.
10. ETags: tests funcionales de `If-Match` correcto, incorrecto y ausente.
11. Reintento opt-in + test.
12. Migraciones y nota en `docs/database-configuration.md`.

## 5. Tests

- **Integration**: lost update antes/después, en SqlServer y PostgreSQL (el
  mapeo difiere, hay que cubrir ambos).
- **Integration**: interacción con soft delete y filtros de tenant.
- **Functional**: 409 y flujo completo de ETag.
- **Unit**: middleware y política de reintento.

## 6. Criterios de aceptación

- [ ] Dos escrituras concurrentes sobre el mismo agregado: la segunda falla con 409.
- [ ] Funciona en SqlServer y PostgreSQL.
- [ ] La versión no aparece en los logs de auditoría.
- [ ] Los agregados sin `HasOptimisticConcurrency` mantienen el comportamiento actual.
- [ ] Ningún handler de aplicación captura excepciones de EF Core.

## 7. Alternativas descartadas

- **`uint Version` incrementado a mano en el agregado**: portable entre motores,
  pero obliga a que cada método de negocio recuerde incrementarlo — error humano
  garantizado. Se puede automatizar en un interceptor, quedando como plan B si
  aparece un tercer proveedor sin token nativo.
- **Bloqueo pesimista (`SELECT ... FOR UPDATE`)**: serializa el acceso y degrada
  el rendimiento; solo justificable en casos puntuales.
- **Comparar todas las columnas como token de concurrencia**: consultas `UPDATE`
  enormes y frágiles ante cambios de esquema.
