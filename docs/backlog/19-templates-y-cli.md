# 19 — Templates `dotnet new` y CLI extendido

> Estado: **Pendiente**
> Ámbito: `tools/Raftel.Cli`, `templates/`
> Rompe API pública: no (aditivo)
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [03](03-paginacion-y-ordenacion.md), [18](18-raftel-testing.md)

## 1. Motivación

El CLI actual (`tools/Raftel.Cli`, publicado como dotnet tool `raftel`) tiene
tres comandos:

```
raftel clean
raftel add command
raftel add query
```

Cubre el escalón más pequeño. Lo que falta es el arranque: **no hay forma de
crear un proyecto Raftel**. El consumidor debe replicar a mano la estructura de
cuatro capas, las referencias entre proyectos, el `RaftelDbContext`, el
`Program.cs` con `AddRaftelApplication` + `AddRaftelData`, el registro de
middlewares en el orden correcto y los proyectos de test.

`templates/` existe pero solo contiene `material` (tema de docfx), no plantillas
de proyecto.

Además, `raftel add` no genera los artefactos de dominio: agregados, value
objects, features completas, ni tests.

## 2. Diseño

### 2.1 Templates `dotnet new`

Paquete `Raftel.Templates` instalable con `dotnet new install Raftel.Templates`.

**`raftel-api`** — solución completa:

```
MyApp/
  src/
    MyApp.Domain/            → agregado de ejemplo, errores, value objects
    MyApp.Application/       → feature de ejemplo (command + query + validator)
    MyApp.Infrastructure/    → DbContext, repositorios, migraciones
    MyApp.Api/               → Program.cs con el pipeline completo
  tests/
    MyApp.Domain.Tests/
    MyApp.Application.IntegrationTests/
    MyApp.Api.FunctionalTests/
    MyApp.ArchitectureTests/   → reglas de Raftel.Testing
  docker-compose.yml
  Directory.Packages.props
  Directory.Build.props        → Nullable enable (ver 14)
  .editorconfig
  MyApp.sln
```

Parámetros del template:

| Parámetro | Valores | Por defecto |
|---|---|---|
| `--database` | `sqlserver`, `postgres` | `postgres` |
| `--multitenancy` | `true`, `false` | `false` |
| `--auth` | `openiddict`, `jwt`, `none` | `openiddict` |
| `--auditing` | `true`, `false` | `true` |
| `--outbox` | `true`, `false` | `false` |
| `--sample-feature` | `true`, `false` | `true` |

Con `--multitenancy false` no se genera `TenantMiddleware` ni las columnas de
tenant: código muerto fuera desde el principio.

**`raftel-module`** — módulo/bounded context añadible a una solución existente.

El `Program.cs` generado debe registrar los middlewares en el **orden correcto**
(logging → validación → autorización → transacción → unidad de trabajo), que es
justo lo que más se equivoca a mano.

### 2.2 CLI extendido

Comandos nuevos, siguiendo el patrón Roslyn ya usado en `AddCommandCommand` y
`AddQueryCommand`:

```
raftel add aggregate <Name> [--feature <Feature>]     # agregado + TypedId + errores + configuración EF
raftel add value-object <Name> [--primitive <type>]   # value object con validación y Result
raftel add feature <Name>                             # carpeta vertical completa: agregado, repo, comandos CRUD, queries, endpoints, tests
raftel add event <Name> --aggregate <Aggregate>       # domain event + handler
raftel add validator <ForType>
raftel add migration <Name>                           # envuelve dotnet ef con el proyecto correcto
raftel new endpoint-group <Name>                      # RouteOptions con AddEndpointGroup
```

Mejoras transversales al CLI actual:

- **Generar el test junto al código.** Hoy `add command` genera comando, handler
  y validador, pero **ningún test**, en un proyecto cuya regla declarada es TDD.
  Debe generar también el test (fallando, con el arrange preparado).
- `--dry-run` para ver qué se generaría.
- Detección del proyecto destino leyendo la solución, no por convención de rutas.
- Comprobación de que el archivo destino no existe (hoy conviene verificar que
  no sobrescribe en silencio).

### 2.3 Consistencia con el framework

Los generadores deben producir código alineado con el estado del backlog:

- Comandos y queries como `sealed record`.
- Handlers `sealed`, con máximo 2-3 dependencias (Object Calisthenics).
- `Result`/`Result<T>` con `Error` tipado ([01](01-error-tipado-y-mapeo-http.md)).
- `CancellationToken` en todas las firmas ([02](02-propagacion-cancellationtoken.md)).
- Queries de listado paginadas por defecto ([03](03-paginacion-y-ordenacion.md)).
- Tests con `Raftel.Testing` ([18](18-raftel-testing.md)).

Cada feature del backlog que cambie una convención **debe** actualizar los
generadores. Añadir esa comprobación al checklist de PR.

### 2.4 Verificación de las plantillas en CI

Riesgo real: las plantillas se pudren en silencio. CI nuevo:

```yaml
- dotnet new install ./templates/Raftel.Templates
- dotnet new raftel-api -n Smoke --database postgres --multitenancy true
- cd Smoke && dotnet build && dotnet test
```

Matriz con las combinaciones principales de parámetros. Sin esto, la plantilla
deja de compilar en la primera feature que toque el `Program.cs`.

## 3. Archivos a tocar

**Nuevos**
- `templates/Raftel.Templates/Raftel.Templates.csproj`
- `templates/Raftel.Templates/content/raftel-api/**` (+ `.template.config/template.json`)
- `templates/Raftel.Templates/content/raftel-module/**`
- `tools/Raftel.Cli/Commands/Add/{AddAggregateCommand,AddValueObjectCommand,AddFeatureCommand,AddEventCommand,AddMigrationCommand}.cs`
- `tools/Raftel.Cli/Generators/**` — extraer la generación Roslyn compartida
- `tests/Raftel.Cli.Tests/**` (hoy el CLI está excluido de cobertura y sin tests)
- `.github/workflows/templates.yml`

**Modificados**
- `tools/Raftel.Cli/Program.cs`
- `tools/Raftel.Cli/Commands/Add/{AddCommandCommand,AddQueryCommand}.cs` — generar tests
- `tools/Raftel.Cli/NamespaceCalculator.cs`
- `intall-cli.cmd` (nota: el nombre tiene una errata — `install-cli.cmd`)

## 4. Plan de implementación

1. **(test)** Tests del CLI actual antes de tocarlo: `add command` genera los
   archivos esperados con el contenido esperado. Hoy no hay ninguno.
2. Refactorizar la generación Roslyn a `Generators/` compartidos.
3. Añadir generación de tests a `add command` / `add query`.
4. Implementar `add aggregate` y `add value-object` (+ tests).
5. Implementar `add feature` componiendo los anteriores.
6. Crear el template `raftel-api` a partir de `demo/`, parametrizado.
7. CI de plantillas con matriz de parámetros.
8. `raftel-module` + `add migration`.
9. Arreglar la errata de `intall-cli.cmd` y documentar la instalación.
10. Documentar todos los comandos en `docs/`.

## 5. Tests

- **Unit**: cada generador produce código que **compila** (verificable con Roslyn
  en memoria, sin escribir a disco).
- **Unit**: no sobrescribe archivos existentes; `--dry-run` no escribe nada.
- **CI**: la plantilla generada compila y sus tests pasan, por combinación de
  parámetros.

## 6. Criterios de aceptación

- [ ] `dotnet new raftel-api` produce una solución que compila y cuyos tests pasan.
- [ ] `raftel add command` genera también el test.
- [ ] El CLI tiene cobertura de tests (hoy está excluido con
      `ExcludeFromCodeCoverage` a nivel de ensamblado).
- [ ] CI verifica las plantillas en cada push.
- [ ] Ningún generador sobrescribe archivos sin avisar.

## 7. Alternativas descartadas

- **Source generators en vez de scaffolding**: el código generado no es editable,
  y aquí se quiere un punto de partida que el desarrollador modifique.
- **Plantillas mantenidas a mano fuera de CI**: se pudren; ya pasa en muchos
  proyectos.
- **Un solo template monolítico sin parámetros**: obliga a borrar a mano lo que
  no se usa (multitenancy, auditoría), que es lo que hace la gente y donde se
  cuelan bugs.
