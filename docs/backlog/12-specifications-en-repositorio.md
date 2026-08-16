# 12 — Specifications en el repositorio e `IRepository` genérico

> Estado: **Pendiente**
> Ámbito: `Raftel.Domain`, `Raftel.Infrastructure`
> Rompe API pública: **sí** (restricción genérica de `IRepository`)
> Depende de: [03](03-paginacion-y-ordenacion.md)

## 1. Motivación

Dos problemas relacionados.

**(a) Las specifications son una feature huérfana.** Existen
`Specification<TModel>`, `AndSpecification`, `OrSpecification` con `ToExpression()`
e `IsSatisfiedBy()`, pero **el repositorio no las acepta**: solo recibe
`Expression<Func<TEntity, bool>>` cruda. El patrón está implementado a medias:
sirve para validar en memoria, no para consultar.

Falta además `NotSpecification` — hay `And` y `Or`, no negación.

Y `IsSatisfiedBy` compila la expresión **en cada llamada**:

```csharp
public bool IsSatisfiedBy(TModel model)
{
    var expression = ToExpression();
    var compiledExpression = expression.Compile();   // ← coste alto, sin caché
    return compiledExpression(model);
}
```

**(b) `IRepository` está sobre-restringido.**

```csharp
public interface IRepository<TEntity, in TId>
    where TEntity : AggregateRoot<TId> where TId : TypedId<Guid>
```

`TId : TypedId<Guid>` obliga a que **toda** clave sea un GUID. Imposible modelar
un agregado con clave `int` (secuencial, típico en tablas de catálogo), `long`
(snowflake) o `string` (código natural). Para un framework de propósito general
es una limitación seria; `TypedId<T>` ya es genérico, la restricción lo desperdicia.

## 2. Diseño

### 2.1 Relajar la restricción de `TId`

```csharp
public interface IRepository<TEntity, in TId>
    where TEntity : AggregateRoot<TId>
    where TId : TypedId
```

Introduciendo un marcador no genérico:

```csharp
public abstract class TypedId
{
    public abstract object Value { get; }
}

public abstract class TypedId<TValue> : TypedId, IEquatable<TypedId<TValue>>
    where TValue : notnull
{
    public TValue TypedValue { get; }
    public override object Value => TypedValue;
}

public abstract class TypedGuidId : TypedId<Guid>;    // el existente, ahora un caso más
```

`Entity<TId>` y `AggregateRoot<TId>` cambian su restricción de `TypedId<Guid>` a
`TypedId`. Es el cambio que rompe: el código existente sigue compilando porque
`TypedGuidId : TypedId<Guid> : TypedId`, pero cualquier restricción explícita
`where TId : TypedId<Guid>` en código de consumidor hay que revisarla.

`EfRepository` necesita ajuste en `GetByIdAsync`: hoy usa `e.Id.Equals(id)`, que
EF traduce mal en algunos casos. Pasar a comparación por el valor primitivo
mediante conversión de valor configurada por entidad.

### 2.2 Completar las specifications

```csharp
public abstract class Specification<TModel>
{
    private Func<TModel, bool>? _compiled;    // caché

    public abstract Expression<Func<TModel, bool>> ToExpression();

    public bool IsSatisfiedBy(TModel model)
    {
        _compiled ??= ToExpression().Compile();
        return _compiled(model);
    }

    public Specification<TModel> And(Specification<TModel> other);
    public Specification<TModel> Or(Specification<TModel> other);
    public Specification<TModel> Not();                       // nuevo
    public static Specification<TModel> All { get; }          // siempre true, elemento neutro
    public static Specification<TModel> None { get; }

    public static Specification<TModel> operator &(...);      // azúcar opcional
    public static Specification<TModel> operator |(...);
    public static Specification<TModel> operator !(...);
}
```

**Bug confirmado en `And`/`Or`.** La implementación actual combina las
expresiones con `Expression.Invoke`:

```csharp
// AndSpecification.ToExpression()
var body = Expression.AndAlso(
    Expression.Invoke(leftExpr, parameter),
    Expression.Invoke(rightExpr, parameter));
```

Funciona en memoria (`IsSatisfiedBy` compila y ejecuta), pero **EF Core no sabe
traducir `InvocationExpression` a SQL**: en cuanto la specification compuesta
llegue a un `Where`, la consulta falla con "The LINQ expression could not be
translated". Es decir, las specifications compuestas son inservibles justo en el
escenario para el que existen.

Solución: reescribir el árbol unificando el parámetro en lugar de invocar.

```csharp
internal sealed class ExpressionParameterRebinder(ParameterExpression parameter) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) => parameter;
}

// AndSpecification
var parameter = Expression.Parameter(typeof(TModel), "entity");
var rebinder = new ExpressionParameterRebinder(parameter);
var body = Expression.AndAlso(
    rebinder.Visit(leftExpr.Body),
    rebinder.Visit(rightExpr.Body));
return Expression.Lambda<Func<TModel, bool>>(body, parameter);
```

El test que lo demuestra debe ejecutar la specification compuesta **contra la
base de datos**, no solo en memoria: en memoria el código actual pasa.

### 2.3 Specifications en el repositorio

```csharp
Task<TEntity?> FirstOrDefaultAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default);
Task<List<TEntity>> ListAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default);
Task<PagedResult<TEntity>> ListPagedAsync(Specification<TEntity> specification, PageRequest page, IReadOnlyList<SortRequest>? sort = null, CancellationToken cancellationToken = default);
Task<bool> AnyAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default);
Task<long> CountAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default);
```

Las sobrecargas con `Expression<Func<TEntity,bool>>` se conservan.

Implementación: `dbContext.Set<TEntity>().Where(specification.ToExpression())`.
Los filtros globales (soft delete, tenant) siguen aplicándose porque son query
filters de EF, no parte de la specification.

### 2.4 Composición de includes (opcional, valorar alcance)

Extensión habitual del patrón: que la specification declare `Include`s. Se deja
**fuera** de esta feature — arrastra `Microsoft.EntityFrameworkCore` hacia
`Raftel.Domain`, prohibido por la regla de dependencias. Alternativa futura:
declarar los includes como `Expression<Func<TEntity, object>>[]` en el dominio y
traducirlos en infraestructura.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Domain/Specifications/NotSpecification.cs`
- `src/Raftel.Domain/Specifications/AllSpecification.cs`, `NoneSpecification.cs`
- `src/Raftel.Domain/Specifications/ExpressionParameterRebinder.cs`
- `tests/Raftel.Domain.Tests/Specifications/SpecificationCompositionTests.cs`
- `tests/Raftel.Infrastructure.Tests/Data/SpecificationQueryTests.cs`

**Modificados**
- `src/Raftel.Domain/BaseTypes/TypedId.cs`, `TypedGuidId.cs`
- `src/Raftel.Domain/BaseTypes/Entity.cs`, `AggregateRoot.cs`
- `src/Raftel.Domain/Abstractions/IRepository.cs`
- `src/Raftel.Domain/Specifications/{Specification,And,Or}Specification.cs`
- `src/Raftel.Infrastructure/Data/EfRepository.cs`
- `BREAKING_CHANGES.md`

## 4. Plan de implementación

1. **(test)** Componer `A.And(B)` y ejecutarla **contra la base de datos** (no
   solo `IsSatisfiedBy`). Si falla por parámetros, el bug está confirmado.
2. Implementar `ExpressionParameterRebinder` y corregir `And`/`Or`.
3. **(test)** `Not()`, `All`, `None`, leyes básicas (`A.And(All) ≡ A`,
   `A.Or(None) ≡ A`, `A.Not().Not() ≡ A` semánticamente).
4. Implementar las specifications que faltan y la caché de compilación.
5. **(test)** `IsSatisfiedBy` llamado 1.000 veces compila una sola vez
   (verificable con un contador en un `ToExpression` instrumentado).
6. **(test)** Un agregado con `TypedId<int>` se persiste y recupera.
7. Introducir `TypedId` no genérico, relajar restricciones, ajustar `EfRepository`.
8. Compilar toda la solución y la demo; ajustar restricciones explícitas.
9. **(test integración)** Métodos nuevos del repositorio con specifications,
   comprobando que los filtros globales siguen aplicándose.
10. Implementar los métodos y migrar al menos un repositorio concreto
    (`UsersRepository`) como ejemplo.
11. Documentar en `BREAKING_CHANGES.md`.

## 5. Tests

- **Unit**: álgebra de specifications, caché de compilación.
- **Integration**: specifications traducidas a SQL correctamente (el test que
  detecta el bug de parámetros solo se manifiesta contra base real).
- **Integration**: agregado con clave `int` y con clave `string`.
- **Architecture test**: `Raftel.Domain` sigue sin referenciar EF Core.

## 6. Criterios de aceptación

- [ ] Una specification compuesta se traduce a SQL sin lanzar.
- [ ] `IsSatisfiedBy` no recompila en cada llamada.
- [ ] Un agregado con clave no-GUID funciona de extremo a extremo.
- [ ] Los filtros globales se aplican también en las consultas por specification.
- [ ] `Raftel.Domain` sin dependencias nuevas.

## 7. Alternativas descartadas

- **Exponer `IQueryable<T>` desde el repositorio**: soluciona todo de golpe pero
  filtra EF Core a la capa de aplicación, permite consultas arbitrarias desde
  handlers y hace imposible testear el repositorio de forma aislada. Contradice
  la regla de dependencias del proyecto.
- **Specifications con `Include` en el dominio**: arrastraría EF Core a `Domain`.
- **Mantener `TypedId<Guid>` obligatorio**: simplifica, pero deja fuera casos de
  uso legítimos y desperdicia que `TypedId<T>` ya sea genérico.

## 8. Breaking changes

- `Entity<TId>` y `AggregateRoot<TId>` relajan la restricción de `TId`. Código de
  consumidor con `where TId : TypedId<Guid>` explícito debe revisarse.
- `TypedId<TValue>` gana una clase base. Serialización personalizada de IDs
  puede requerir ajuste.
