# Plan de implementación: Queries paginadas

> Estado: **Propuesta** · Rama: `claude/vibrant-thompson-bivm3z` · Última actualización: 2026-06-21

## 1. Contexto y problema

Tras el análisis del repositorio se ha detectado un riesgo de escalabilidad en la capa de
lectura de datos:

- `IRepository<TEntity, TId>.ListAllAsync(...)` materializa **siempre la colección completa**
  (`Task<List<TEntity>>`), sin límite de filas.
- Las queries que listan colecciones siguen ese mismo patrón. El caso paradigmático es
  `GetAllTenantsQuery` → `GetAllTenantsQueryHandler`, que hace
  `tenantsRepository.ListAllAsync(...)` y proyecta **todo** a `List<GetAllTenantsResponse>`.
- En un escenario multi-tenant en producción, conforme crezca el volumen de datos (tenants,
  usuarios, agregados de negocio), traer la tabla entera en cada request degrada memoria,
  latencia y consumo de DB sin techo.

El propio handler de demo `GetPirateByFilterQueryHandler` agrava el patrón: hace
`ListAllAsync` y filtra **en memoria** con `WhereIf`, en lugar de empujar el filtro y la
paginación a SQL.

### Archivos implicados (mapa actual)

| Capa | Archivo | Rol actual |
| --- | --- | --- |
| Domain | `src/Raftel.Domain/Abstractions/IRepository.cs` | Contrato `ListAllAsync` → `List<TEntity>` |
| Domain | `src/Raftel.Domain/Abstractions/Result.cs` | `Result`/`Result<T>` (envoltura de retorno) |
| Infrastructure | `src/Raftel.Infrastructure/Data/EfRepository.cs` | Implementación EF de `ListAllAsync` |
| Application | `src/Raftel.Application/Features/Tenants/GetAllTenants/*` | Query + Handler + Response |
| Api.Server | `src/Raftel.Api.Server/AutoEndpoints/QueryEndpointMapper.cs` | Bind de params query/route → record |
| Api.Server | `src/Raftel.Api.Server/AutoEndpoints/ApiParametersBuilder.cs` | Generación OpenAPI de parámetros |
| Api.Client | `src/Raftel.Api.Client/QueryFilter.cs` | Serialización de filtros a querystring |

## 2. Objetivos

1. Introducir una primitiva de dominio `PagedResult<T>` en `Raftel.Domain.Abstractions`,
   **sin dependencias** a otras capas ni a EF Core.
2. Introducir un value object de entrada `PaginationRequest` (skip/take + opción de orden)
   reutilizable por cualquier query que liste colecciones.
3. Dar soporte de paginación en `IRepository` **sin romper** el `ListAllAsync` existente:
   métodos nuevos, no sustitución.
4. Implementar la paginación en `EfRepository` empujando `Skip/Take/Count` a SQL.
5. Migrar `GetAllTenantsQuery` como **caso de referencia** y dejar documentado el patrón.
6. Permitir que cada repositorio cree métodos paginados personalizados (filtro + orden
   específicos del agregado) reutilizando la misma primitiva.
7. Mantener AutoEndpoints + `QueryFilter` simétricos: el cliente debe saber serializar
   `pageNumber`/`pageSize` que el servidor infiere del constructor del record.

### No-objetivos (de momento)

- No se elimina `ListAllAsync`; sigue siendo válido para catálogos pequeños y acotados.
- No se implementa paginación por cursor en la primera fase (ver §6, fase 2).
- No se modifica el contrato de `Result<T>` (la paginación viaja **dentro** del valor).

## 3. Decisiones de diseño

### 3.1 Estrategia de paginación: offset primero, cursor después

Se opta por un enfoque por fases:

- **Fase 1 — Offset (`skip`/`take`)**: cubre el 90% de los casos (paginación clásica con
  número de página y total de páginas). Es lo que encaja directamente con los parámetros que
  AutoEndpoints ya sabe inferir desde el constructor del record (tipos simples).
- **Fase 2 — Cursor/keyset (opcional)**: para colecciones muy grandes o scroll infinito,
  donde `OFFSET` grande es costoso. Se añade como **modo adicional**, no sustituye al offset.

> Justificación: el offset es trivial de exponer en querystring (`?pageNumber=2&pageSize=20`),
> es lo que el `QueryFilter` actual serializa sin cambios, y mantiene la API homogénea. El
> cursor se reserva para casos concretos que lo justifiquen, evitando complejidad prematura.

### 3.2 Métodos nuevos vs. sobrecarga

Se añaden **métodos nuevos** (`ListPagedAsync`) en lugar de cambiar la firma de
`ListAllAsync`. Razones:

- Cero impacto en los llamadores actuales (no rompe build ni tests existentes).
- Hace explícita la intención en el call-site (`ListPagedAsync` deja claro que hay límite).
- Permite que cada repositorio exponga métodos paginados a medida (p. ej.
  `ITenantsRepository.SearchAsync(filtro, PaginationRequest)`), reutilizando la primitiva.

### 3.3 Dónde vive cada tipo

| Tipo | Proyecto / namespace | Motivo |
| --- | --- | --- |
| `PagedResult<T>` | `Raftel.Domain/Abstractions` | Primitiva de dominio sin dependencias, retorno de queries |
| `PaginationRequest` | `Raftel.Domain/Abstractions` | VO de entrada, validado fail-fast (calisthenics: wrap primitives) |
| `IRepository.ListPagedAsync` | `Raftel.Domain/Abstractions` | Contrato |
| `EfRepository.ListPagedAsync` | `Raftel.Infrastructure/Data` | Única capa que conoce EF Core |

## 4. Diseño de los tipos

### 4.1 `PaginationRequest` (entrada)

Value object inmutable que envuelve los primitivos de paginación y se valida en construcción
(fail-fast, coherente con `Email`/`Code`). Normaliza valores fuera de rango en lugar de
lanzar, para no convertir un parámetro de UI en un error 500.

```csharp
namespace Raftel.Domain.Abstractions;

public sealed record PaginationRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public int PageNumber { get; }   // 1-based
    public int PageSize { get; }

    public PaginationRequest(int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };
    }

    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;
}
```

> `MaxPageSize` es la salvaguarda clave: aunque el cliente pida `pageSize=1000000`, el
> repositorio nunca traerá más de `MaxPageSize` filas. Este tope es el que neutraliza el
> riesgo de escalabilidad señalado.

### 4.2 `PagedResult<T>` (salida)

```csharp
namespace Raftel.Domain.Abstractions;

public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public long TotalCount { get; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, long totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), PageNumber, PageSize, TotalCount);
}
```

> `Map` permite proyectar de entidad a DTO conservando los metadatos de paginación, de modo
> que el handler hace una sola línea (ver §5.3).

### 4.3 Contrato en `IRepository`

```csharp
Task<PagedResult<TEntity>> ListPagedAsync(
    PaginationRequest pagination,
    Expression<Func<TEntity, bool>>? filter = null,
    CancellationToken cancellationToken = default);
```

- Se mantiene `ListAllAsync` intacto.
- Para orden estable (requisito de offset fiable), la implementación base ordena por
  `Id` por defecto. Los repositorios que necesiten otro orden expondrán un método propio
  (ver §3.2 y §6).

### 4.4 Implementación en `EfRepository`

```csharp
public async Task<PagedResult<TEntity>> ListPagedAsync(
    PaginationRequest pagination,
    Expression<Func<TEntity, bool>>? filter = null,
    CancellationToken cancellationToken = default)
{
    var query = dbContext.Set<TEntity>().WhereIf(filter is not null, filter);

    var totalCount = await query.LongCountAsync(cancellationToken);

    var items = await query
        .OrderBy(e => e.Id)               // orden estable para offset determinista
        .Skip(pagination.Skip)
        .Take(pagination.Take)
        .ToListAsync(cancellationToken);

    return new PagedResult<TEntity>(items, pagination.PageNumber, pagination.PageSize, totalCount);
}
```

> `LongCountAsync` + `Skip/Take` se traducen a `COUNT(*)` y `OFFSET/FETCH` en SQL; nunca se
> materializa la tabla completa. Los filtros globales (soft-delete + tenant) ya aplicados por
> `RaftelDbContext` se combinan automáticamente, así que el `COUNT` también respeta el tenant.

## 5. Plan por capas (orden de ejecución, TDD)

Cada paso: test primero, implementación después, commit (Conventional Commits).

### 5.1 Domain — primitivas

1. `test`: `PaginationRequestTests` (normalización de page<1, size>Max, Skip/Take).
2. `test`: `PagedResultTests` (TotalPages, HasNext/Previous, `Map`).
3. `feat`: añadir `PaginationRequest` y `PagedResult<T>` en `Abstractions`.
4. `feat`: añadir `ListPagedAsync` a `IRepository`.

### 5.2 Infrastructure — EfRepository

1. `test` (integración con Testcontainers, ver `EfRepositoryTests`/`PostgreSqlEfRepositoryTests`):
   - devuelve la página correcta y `TotalCount` correcto;
   - respeta `MaxPageSize`;
   - respeta filtro global de tenant (no cuenta filas de otros tenants).
2. `feat`: implementar `ListPagedAsync` en `EfRepository`.

### 5.3 Application — caso de referencia `GetAllTenants`

Migrar la query existente a un contrato paginado. Propuesta de firma:

```csharp
// GetAllTenantsQuery.cs
[RequiresPermission(TenantsPermissions.View)]
public sealed record GetAllTenantsQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResult<GetAllTenantsResponse>>;
```

```csharp
// GetAllTenantsQueryHandler.cs
public async Task<Result<PagedResult<GetAllTenantsResponse>>> HandleAsync(
    GetAllTenantsQuery request, CancellationToken token = default)
{
    var pagination = new PaginationRequest(request.PageNumber, request.PageSize);
    var page = await tenantsRepository.ListPagedAsync(pagination, cancellationToken: token);

    return page.Map(tenant => new GetAllTenantsResponse
    {
        Id = tenant.Id,
        Name = tenant.Name,
        Code = tenant.Code,
        Description = tenant.Description
    });
}
```

- `test`: actualizar `GetAllTenantsQueryHandlerTests` (sustituir mock de `ListAllAsync` por
  `ListPagedAsync`, verificar metadatos de página propagados).

> Decisión de compatibilidad: cambiar el tipo de retorno de `List<...>` a
> `PagedResult<...>` es **breaking** para los consumidores del endpoint. Como es una mejora de
> contrato intencionada y `PageNumber`/`PageSize` tienen valores por defecto, una llamada sin
> parámetros sigue funcionando (devuelve la primera página). Ver §7 (riesgos) antes de aplicar.

### 5.4 Api.Server — AutoEndpoints

`QueryEndpointMapper.BuildRequestFromRouteAndQuery` ya itera los parámetros del constructor y
`ApiParametersBuilder` ya genera parámetros OpenAPI desde las propiedades del record. Como
`PageNumber`/`PageSize` son `int` (tipos simples ya soportados por `ConvertSimpleType`):

- **No requiere cambios de código** para que `?pageNumber=2&pageSize=20` funcione.
- Verificar en `PiratesEndpointsTests` / funcionales que los parámetros se infieren y
  documentan en OpenAPI.
- (Opcional) Añadir helper `PaginationRequest` como par de parámetros estándar para no repetir
  `PageNumber`/`PageSize` en cada record — ver §6, mejora futura.

### 5.5 Api.Client — QueryFilter

`QueryFilter.FromObject` ya serializa cualquier propiedad pública a querystring en camelCase.
Un objeto `{ PageNumber = 2, PageSize = 20 }` produce `?pageNumber=2&pageSize=20`
automáticamente, simétrico con el servidor.

- `test`: añadir caso en `QueryFilterTests` cubriendo parámetros de paginación.

### 5.6 Demo — alinear el ejemplo

Migrar `GetPirateByFilterQuery`/Handler para que:

- exponga `PageNumber`/`PageSize`;
- empuje el filtro a SQL en un método propio del repositorio (`IPirateRepository`) en vez de
  filtrar en memoria con `WhereIf` tras `ListAllAsync` (ver §6).

## 6. Métodos personalizados por repositorio

La primitiva habilita que cada agregado defina búsquedas paginadas con su filtro/orden:

```csharp
public interface IPirateRepository : IRepository<Pirate, PirateId>
{
    Task<PagedResult<Pirate>> SearchAsync(
        string? name, decimal? maxBounty, PaginationRequest pagination, CancellationToken token);
}
```

Implementación en Infrastructure construyendo el `Expression` y delegando en la lógica
`Skip/Take/Count` (puede reutilizar un helper `protected` extraído de `ListPagedAsync` que
acepte un `IQueryable<TEntity>` ya ordenado, para permitir orden por columnas distintas de
`Id`).

> Patrón recomendado: extraer en `EfRepository` un `protected` helper
> `ToPagedResultAsync(IQueryable<TEntity> orderedQuery, PaginationRequest, CancellationToken)`
> que repositorios concretos invoquen con su propio `Where`/`OrderBy`. Así el orden estable
> deja de estar fijado a `Id` cuando el agregado necesita otro criterio.

## 7. Riesgos y mitigaciones

| Riesgo | Mitigación |
| --- | --- |
| Cambio breaking del contrato de `GetAllTenantsQuery` (de `List` a `PagedResult`) | Confirmar con el equipo; defaults en `PageNumber`/`PageSize` mantienen llamadas sin params operativas. Alternativa: nueva query paginada y deprecar la antigua. |
| `OFFSET` grande es costoso en tablas enormes | Topar `MaxPageSize`; fase 2 con keyset/cursor para esos casos. |
| Orden por `Id` no es el esperado por la UI | Helper `ToPagedResultAsync` con `IQueryable` ya ordenado por repositorio. |
| Doble round-trip (`COUNT` + página) | Aceptable; si se vuelve cuello de botella, evaluar `CountAsync` cacheado o estimación. |
| Filtros en memoria preexistentes (demo) | Migrar a `Expression` empujado a SQL en el mismo PR del demo. |

## 8. Checklist de entregables

- [ ] `PaginationRequest` + tests (Domain)
- [ ] `PagedResult<T>` + tests (Domain)
- [ ] `IRepository.ListPagedAsync` (Domain)
- [ ] `EfRepository.ListPagedAsync` + helper `ToPagedResultAsync` + tests integración (Infra)
- [ ] Migración `GetAllTenants` (Application) + tests
- [ ] Verificación AutoEndpoints + OpenAPI (Api.Server) + funcionales
- [ ] Caso paginación en `QueryFilterTests` (Api.Client)
- [ ] Migración demo `GetPirateByFilter` a búsqueda paginada en SQL
- [ ] Actualizar `CLAUDE.md` de Domain/Infrastructure documentando el patrón paginado

## 9. Fase 2 (futuro): paginación por cursor

Para colecciones muy grandes / scroll infinito, añadir un modo keyset sin tocar el offset:

- `CursorPaginationRequest(string? cursor, int pageSize)` donde `cursor` codifica el último
  `Id` (UUIDv7, ya ordenable temporalmente — encaja con `Guid.CreateVersion7()`).
- `CursorPagedResult<T>(IReadOnlyList<T> items, string? nextCursor)`.
- Repositorio: `WHERE Id > @cursor ORDER BY Id LIMIT @take`, sin `OFFSET`.

Se documentará en un plan aparte cuando un caso real lo justifique.
