# Plan: eliminar la duplicación de tests de integración multi-motor (SQL Server + PostgreSQL)

## Objetivo

Raftel soporta conectarse a **SQL Server** y **PostgreSQL**. Hoy, para cubrir ambos
motores, los tests de integración se **duplican**: existe una clase de test por motor,
con el mismo cuerpo de test copiado y un sufijo `_WithPostgreSql` en los nombres.

Queremos **escribir el cuerpo del test una sola vez** y que se ejecute contra **ambos
motores**, reduciendo el código a mantener sin perder cobertura.

---

## Situación actual (inventario de la duplicación)

### Infraestructura de test triplicada por motor

| Pieza | SQL Server | PostgreSQL |
|------|------------|------------|
| Container fixture | `SqlServerTestContainerFixture` | `PostgreSqlTestContainerFixture` |
| Collection | `SqlServerTestCollection` | `PostgreSqlTestCollection` |
| Base de test | `InfrastructureTestBase` | `PostgreSqlInfrastructureTestBase` |

Las dos *bases* son **idénticas** salvo por tres puntos:
1. El tipo de fixture inyectado.
2. `AddSampleInfrastructure(conn)` vs `AddSampleInfrastructure(conn, DatabaseProvider.PostgreSql)`.
3. `new SqlConnection` + `DbAdapter.SqlServer` vs `new NpgsqlConnection` + `DbAdapter.Postgres` (Respawn).

> `Raftel.Application.IntegrationTests` reutiliza `SqlServerTestContainerFixture` vía
> referencia de proyecto y tiene su propia `IntegrationTestBase` (solo SQL Server).
> `Raftel.Api.FunctionalTests` levanta su propio `MsSqlContainer` en `ApiTestFactory`
> (también solo SQL Server). Ambos heredarían cualquier abstracción que creemos.

### Cuerpos de test duplicados (la duplicación cara)

| Comportamiento | SQL Server | PostgreSQL | ¿Cuerpo idéntico? |
|----------------|------------|------------|-------------------|
| Repositorio EF | `Data/EfRepositoryTests.cs` (7) | `Data/PostgreSqlEfRepositoryTests.cs` (8) | Sí (PG tiene 1 test extra) |
| Multitenancy de usuarios | `Data/Features/Users/UsersMultitenancyTests.cs` (12) | `Data/Features/Users/PostgreSqlUsersMultitenancyTests.cs` (2) | Parcial |
| Interceptor de cambios | `Data/Auditing/EntityChangesTrackerInterceptorTests.cs` (9) | `Data/Auditing/EntityChangesTrackerInterceptorPostgreSqlTests.cs` (4) | Parcial |

El resto de clases (`UnitOfWorkTests`, `TenantsRepositoryTests`, `UsersSoftDeleteTests`,
`MultitenancyTests`, `DatabaseProviderTests`, `UserAuditingTests`, auditoría pura…)
hoy solo corren contra **SQL Server**; con la solución elegida pasarían a correr en
ambos motores **gratis**, aumentando cobertura.

### Restricción técnica clave

- **xUnit 2.9.3 (v2)** (ver `Directory.Packages.props`). Esto descarta algunas
  facilidades de paralelización/fixtures de xUnit v3 y condiciona las opciones.
- Cada motor usa **un contenedor por ensamblado** (no por test) + reset con **Respawn**
  en `InitializeAsync`. Hay que preservar este modelo para no disparar el tiempo de CI.

---

## Opciones

### Opción A — Clase base abstracta de test + clases derivadas finas por motor (Template Method)

El cuerpo de cada test vive **una vez** en una clase base abstracta. Por cada motor se
crea una clase derivada **trivial** (~8 líneas) que solo aporta `[Collection]` y el
fixture. xUnit descubre y ejecuta los `[Fact]` heredados en cada derivada.

```csharp
// Cuerpo escrito UNA vez
public abstract class EfRepositoryTestsBase : InfrastructureTestBase
{
    protected EfRepositoryTestsBase(IDbContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetByIdAsync_ShouldRetrieveExpectedData() { /* ... */ }
    // resto de tests...
}

// Wiring por motor (lo único duplicado, mínimo y mecánico)
[Collection(SqlServerTestCollection.Name)]
public sealed class SqlServerEfRepositoryTests(SqlServerTestContainerFixture f)
    : EfRepositoryTestsBase(f);

[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlEfRepositoryTests(PostgreSqlTestContainerFixture f)
    : EfRepositoryTestsBase(f);
```

Requiere antes **unificar la base** en una sola `InfrastructureTestBase` parametrizada
por una abstracción `IDbContainerFixture` que exponga `ConnectionString`,
`DatabaseProvider` y el `DbAdapter` de Respawn (elimina `PostgreSqlInfrastructureTestBase`).

- ✅ Compatible 100% con xUnit v2, sin magia.
- ✅ Elimina la duplicación real (los cuerpos de test) y unifica las dos bases en una.
- ✅ Reutiliza el modelo actual de contenedor-por-ensamblado + Respawn sin cambios.
- ✅ Permite excluir un motor puntual (no creas la derivada) o añadir tests específicos
  de un motor en su derivada.
- ⚠️ Sigue habiendo 2 clases *triviales* por comportamiento (solo wiring, sin lógica).
- ⚠️ Refactor mecánico de todas las clases de test existentes.

### Opción B — Tests data-driven: `[Theory]` con matriz de proveedores (una sola clase)

Una única clase de test; cada `[Fact]` pasa a `[Theory]` y recibe el proveedor como
dato. El `ServiceProvider`/`DbContext` se construye **dentro** del test (o vía helper)
según el proveedor recibido.

```csharp
[CollectionDefinition(Name)]
public sealed class DatabasesCollection
    : ICollectionFixture<SqlServerTestContainerFixture>,
      ICollectionFixture<PostgreSqlTestContainerFixture> { public const string Name = "Databases"; }

[Collection(DatabasesCollection.Name)]
public class EfRepositoryTests
{
    public static IEnumerable<object[]> Providers =>
        [[DatabaseProvider.SqlServer], [DatabaseProvider.PostgreSql]];

    [Theory, MemberData(nameof(Providers))]
    public async Task GetByIdAsync_ShouldRetrieveExpectedData(DatabaseProvider provider)
    {
        await using var ctx = await _harness.CreateAsync(provider); // build SP + respawn
        // ... cuerpo único
    }
}
```

- ✅ Máxima reducción: **una sola clase** corre en ambos motores; cada caso aparece como
  dos filas en el runner (`...(provider: SqlServer)` / `...(provider: PostgreSql)`).
- ✅ Añadir un tercer motor = añadir una fila.
- ⚠️ Cambia el modelo de ciclo de vida: `IAsyncLifetime` (init/respawn por instancia) ya
  no encaja porque el proveedor es por *fila*, no por instancia → hay que mover la
  construcción del SP y el Respawn a un *harness* invocado dentro del test (`using`).
- ⚠️ La colección debe declarar **ambos** `ICollectionFixture<>`, por lo que **ambos
  contenedores arrancan siempre**, aunque filtres por un motor.
- ⚠️ Refactor más invasivo de las firmas y del manejo de scope/SP en cada test.

### Opción C — Migrar a xUnit v3 y usar fixtures de ensamblado + theory nativa

xUnit v3 mejora la parametrización (assembly fixtures, `ITheoryDataRow`, mejor control
de paralelismo) y permitiría una matriz de proveedores más limpia que en v2.

- ✅ La solución más “nativa” a medio plazo.
- ⚠️ Migración de **todos** los proyectos de test (breaking changes de v2→v3: runner,
  paquetes, `IAsyncLifetime`, etc.). Riesgo y alcance altos para el problema concreto.
- ⚠️ No resuelve por sí sola la duplicación: aun en v3 hay que aplicar A o B.
- ❌ Desproporcionado como primer paso; valorar solo como evolución futura.

### Opción D — Mantener la duplicación (statu quo)

- ❌ Es justo lo que queremos eliminar. Listada solo como línea base de comparación.

---

## Recomendación

**Opción A (clase base abstracta + derivadas finas)** como primer paso, precedida de la
**unificación de la base** en una sola `InfrastructureTestBase` parametrizada por
`IDbContainerFixture`.

Motivos:
- Resuelve la duplicación **real** (cuerpos de test) con **riesgo bajo** y sin tocar el
  modelo de contenedores/Respawn que ya funciona.
- Es 100% compatible con xUnit v2; no obliga a una migración mayor.
- El wiring por motor que queda es trivial, mecánico y revisable de un vistazo, y permite
  incluir/excluir motores o añadir tests específicos de un motor sin fricción.
- Como efecto secundario, sube la cobertura: clases hoy solo-SQL-Server correrán en ambos.

La **Opción B** es atractiva si se prioriza “una sola clase por comportamiento”, pero el
cambio de ciclo de vida (perder `IAsyncLifetime` por instancia) y el arranque obligatorio
de ambos contenedores la hacen más invasiva. Es una buena **evolución posterior** una vez
estabilizada A. La **Opción C** se deja como hoja de ruta futura, desacoplada de este
problema.

---

## Plan de implementación (Opción A)

1. **Abstracción de fixture.** Crear `IDbContainerFixture` con `ConnectionString`,
   `Provider` (`DatabaseProvider`) y `RespawnAdapter` (`DbAdapter`). Implementarlo en
   `SqlServerTestContainerFixture` y `PostgreSqlTestContainerFixture` (cambio mínimo).
2. **Unificar la base.** Refactorizar `InfrastructureTestBase` para recibir
   `IDbContainerFixture` y usar `Provider`/`RespawnAdapter` y la `DbConnection` correcta
   (helper que devuelva `SqlConnection`/`NpgsqlConnection` según proveedor). **Eliminar
   `PostgreSqlInfrastructureTestBase`.**
3. **Extraer bases abstractas de test.** Por cada comportamiento duplicado, mover el
   cuerpo a `XxxTestsBase : InfrastructureTestBase` y crear las dos derivadas finas
   (`SqlServer…` / `PostgreSql…`) con su `[Collection]` y fixture. Empezar por
   `EfRepositoryTests` (par limpio y representativo) como prueba de concepto.
4. **Consolidar los pares parciales.** En `UsersMultitenancy` y
   `EntityChangesTrackerInterceptor`, mover el tronco común a la base y dejar en las
   derivadas únicamente los tests específicos de cada motor (si los hay).
5. **Ampliar cobertura.** Las clases hoy solo-SQL-Server pasan a heredar de su base
   abstracta y obtienen su derivada PostgreSQL.
6. **Replicar el patrón** en `Raftel.Application.IntegrationTests` (su `IntegrationTestBase`
   pasa a parametrizarse por `IDbContainerFixture`).
7. **Valorar `Api.FunctionalTests`.** Parametrizar `ApiTestFactory` por proveedor
   (matriz de factories) es un trabajo aparte; documentar si entra en alcance o no.
8. **Verificación.** `dotnet test` debe pasar en ambos motores; comprobar en el runner
   que cada caso aparece para SQL Server y PostgreSQL, y que el tiempo total no se dispara
   (un contenedor por motor por ensamblado).
9. **Documentación.** Actualizar `tests/Raftel.Infrastructure.Tests/CLAUDE.md` y
   `tests/Raftel.Application.IntegrationTests/CLAUDE.md` con el nuevo patrón base/derivadas.

## Criterios de aceptación

- El cuerpo de cada test de integración se escribe **una sola vez**.
- Cada comportamiento se ejecuta contra **SQL Server y PostgreSQL**.
- No quedan clases `Postgre…`/`…PostgreSql` con cuerpos de test copiados.
- Se mantiene el modelo de **un contenedor por motor por ensamblado** + Respawn.
- La suite pasa en verde y la cobertura por motor es igual o mayor que la actual.
