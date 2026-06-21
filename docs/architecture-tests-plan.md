# Plan de implementación: Tests de arquitectura (NetArchTest)

> **Objetivo**: cerrar la discrepancia entre lo que prescriben las instrucciones internas
> (`.github/instructions/clean-architecture.instructions.md`) y el estado real del repositorio,
> añadiendo tests automáticos que validen las dependencias entre capas y prohíban dependencias
> indebidas (p. ej. EF Core en `Domain`, `Application` o `Api`).

## 1. Contexto y motivación

Las instrucciones de Clean Architecture del propio repo prescriben **explícitamente** un
`ArchitectureTests.cs` con [NetArchTest](https://github.com/BenMorris/NetArchTest) para validar
la regla de dependencias entre capas. Sin embargo, hoy:

- ❌ `NetArchTest` **no** figura en `Directory.Packages.props`.
- ❌ **No existe** ningún fichero `ArchitectureTests.cs` en `tests/`.

Esto es una discrepancia clara entre documentación y código. Un test de arquitectura es barato de
escribir y previene **regresiones de capa** a futuro (es una de las mejoras con mejor relación
esfuerzo/beneficio del repo).

La regla de dependencias (de `CLAUDE.md` y de las instrucciones) es **innegociable**:

- **Domain** → sin dependencias.
- **Application** → depende solo de **Domain**.
- **Infrastructure** → depende de **Application** y **Domain**.
- **Api** → depende de **Infrastructure** (que arrastra Application y Domain).
- `Microsoft.EntityFrameworkCore` está **prohibido** en `Domain` y en la capa `Api`.

## 2. Hallazgo importante: framework ≠ app concreta

El ejemplo de las instrucciones está escrito para una **aplicación concreta** (`Order.Api` con
`Program.cs` que solo depende de `Application`). Raftel **no es una app**: es un **micro-framework**
distribuido como librerías. Las referencias reales son:

| Proyecto (`src/`)        | Referencias de proyecto             | EF Core |
|--------------------------|-------------------------------------|---------|
| `Raftel.Domain`          | *(ninguna)*                         | No      |
| `Raftel.Application`     | `Domain`                            | No      |
| `Raftel.Infrastructure`  | `Application`, `Domain`, `Shared`   | **Sí**  |
| `Raftel.Api.Server`      | `Application`, `Domain`, `Shared`   | No      |

> ⚠️ **`Raftel.Api.Server` referencia `Application` y `Domain` directamente, NO `Infrastructure`.**
> Es correcto: `Api.Server` es una librería que genera endpoints (AutoEndpoints) sobre las
> abstracciones de `Application`. El host real (con `Program.cs`) vive en `demo/` y es quien compone
> `Infrastructure`.

**Conclusión**: no se puede copiar la regla literal "Api solo depende de Application". Hay que
codificar las reglas que son **verdaderas y deseables** para el framework. La regla de mayor valor
y siempre válida es **la prohibición de EF Core** en `Domain`, `Application` y `Api.Server`.

## 3. Reglas a implementar

| # | Regla | Tipo |
|---|-------|------|
| R1 | `Raftel.Domain` no depende de `Application`, `Infrastructure` ni `Api.Server` | Capas |
| R2 | `Raftel.Domain` no depende de `Microsoft.EntityFrameworkCore` | Prohibición |
| R3 | `Raftel.Application` no depende de `Infrastructure` ni de `Api.Server` | Capas |
| R4 | `Raftel.Application` no depende de `Microsoft.EntityFrameworkCore` | Prohibición |
| R5 | `Raftel.Api.Server` no depende de `Infrastructure` | Capas |
| R6 | `Raftel.Api.Server` no depende de `Microsoft.EntityFrameworkCore` | Prohibición |
| R7 | `Raftel.Application` no depende de ASP.NET Core (`Microsoft.AspNetCore.*`) | Prohibición *(opcional)* |

> R7 es opcional pero refuerza que la lógica de aplicación se mantenga agnóstica del transporte HTTP.
> EF Core en `Infrastructure` **sí** está permitido (es su responsabilidad), por lo que no se testea.

## 4. Decisiones de diseño

### 4.1 Ubicación del proyecto de tests

Las capas a validar son varias (`Domain`, `Application`, `Infrastructure`, `Api.Server`) y ningún
proyecto de tests actual referencia a todas. Se crea un **proyecto de tests dedicado**:

```
tests/Raftel.ArchitectureTests/
  Raftel.ArchitectureTests.csproj
  ArchitectureTests.cs
```

**Por qué un proyecto propio** (y no reutilizar `Raftel.Infrastructure.Tests`):

- Necesita referenciar `Domain`, `Application`, `Infrastructure` **y** `Api.Server` a la vez.
- Mantiene los tests de arquitectura aislados, rápidos y sin dependencias de Testcontainers/DB.
- Encaja con la nomenclatura de las instrucciones (`ArchitectureTests.cs`).

### 4.2 Resolución de assemblies (sin ficheros marcador)

El ejemplo de las instrucciones usa ficheros marcador (`DomainReference.cs`, etc.). Para **no tocar
`src/`**, se resuelven los assemblies vía `typeof()` de un tipo público y estable de cada capa:

| Capa            | Tipo ancla sugerido (verificar antes de usar)                 |
|-----------------|---------------------------------------------------------------|
| Domain          | `Raftel.Domain.BaseTypes.AggregateRoot<>` o un tipo de `Abstractions` |
| Application     | `Raftel.Application.RaftelApplicationBuilder`                  |
| Infrastructure  | `Raftel.Infrastructure.DependencyInjection`                   |
| Api.Server      | un tipo público de `Raftel.Api.Server.AutoEndpoints`          |

> Antes de fijar los anclas, confirmar que el tipo es `public`. Si alguno fuera `internal`, la
> alternativa es añadir un marcador `public sealed class XxxReference;` en cada `csproj` de `src/`
> (coste mínimo y alineado literalmente con las instrucciones).

### 4.3 Paquete y versión

Añadir a `Directory.Packages.props` (gestión centralizada de versiones ya activa):

```xml
<PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
```

> Verificar en NuGet la última versión estable compatible con `net10.0` en el momento de
> implementar. `NetArchTest.Rules` es la librería base (sin acoplar a un runner concreto).

## 5. Pasos de implementación (TDD-friendly)

1. **Añadir el paquete** `NetArchTest.Rules` a `Directory.Packages.props`.
2. **Crear el proyecto** `tests/Raftel.ArchitectureTests/Raftel.ArchitectureTests.csproj`:
   - `TargetFramework` `net10.0`, `IsTestProject=true`, `IsPackable=false`.
   - `PackageReference`: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`,
     `Shouldly`, `coverlet.collector`, `NetArchTest.Rules`.
   - `ProjectReference` a `Raftel.Domain`, `Raftel.Application`, `Raftel.Infrastructure`,
     `Raftel.Api.Server`.
   - Copiar el bloque `ExcludeFromCodeCoverage` y `<Using Include="Xunit"/>` del resto de tests.
3. **Registrar el proyecto** en `Raftel.sln` (bajo la carpeta de solución `tests`).
4. **Escribir `ArchitectureTests.cs`** con un `[Fact]` por cada regla R1–R6 (R7 opcional).
   - Usar `Shouldly` para los asserts (`result.IsSuccessful.ShouldBeTrue(...)`), coherente con el
     resto del repo (ver commit `1ef61fc`, que migró asserts a Shouldly).
   - En el mensaje de fallo, incluir `result.FailingTypeNames` para diagnóstico.
5. **Ejecutar** `dotnet test tests/Raftel.ArchitectureTests` y verificar que todo pasa en verde.
6. **(Opcional) Verificación negativa temporal**: introducir a propósito un `using` prohibido para
   confirmar que el test falla, y luego revertirlo. Da confianza de que las reglas "muerden".
7. **Build completo**: `dotnet build Raftel.sln` y `dotnet test`.

## 6. Esqueleto de referencia

```csharp
using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace Raftel.ArchitectureTests;

public class ArchitectureTests
{
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";

    private const string DomainNamespace = "Raftel.Domain";
    private const string ApplicationNamespace = "Raftel.Application";
    private const string InfrastructureNamespace = "Raftel.Infrastructure";
    private const string ApiNamespace = "Raftel.Api.Server";

    // Confirmar que los tipos ancla son public antes de usarlos.
    private static readonly Assembly DomainAssembly = typeof(Raftel.Domain.BaseTypes.AggregateRoot<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Raftel.Application.RaftelApplicationBuilder).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Raftel.Infrastructure.DependencyInjection).Assembly;
    // private static readonly Assembly ApiAssembly = typeof(/* tipo de AutoEndpoints */).Assembly;

    [Fact] // R1
    public void Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain no debe depender de otras capas. Tipos infractores: {Format(result)}");
    }

    [Fact] // R2
    public void Domain_Should_Not_DependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should().NotHaveDependencyOn(EntityFrameworkCore)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"EF Core prohibido en Domain. {Format(result)}");
    }

    [Fact] // R3
    public void Application_Should_Not_DependOn_InfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Application solo debe depender de Domain. {Format(result)}");
    }

    [Fact] // R4
    public void Application_Should_Not_DependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should().NotHaveDependencyOn(EntityFrameworkCore)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue($"EF Core prohibido en Application. {Format(result)}");
    }

    // R5 y R6: equivalentes sobre ApiAssembly (no Infrastructure, no EF Core).

    private static string Format(TestResult result) =>
        result.FailingTypeNames is null ? "-" : string.Join(", ", result.FailingTypeNames);
}
```

## 7. Riesgos y consideraciones

- **`NetArchTest` analiza dependencias a nivel de IL**: detecta usos reales de tipos, no solo
  referencias de proyecto. Esto es lo que queremos, pero puede aflorar acoplamientos no evidentes.
  Si R5 (Api → Infrastructure) fallara, sería un hallazgo legítimo a corregir, no un falso positivo.
- **No testear EF Core en `Infrastructure`**: ahí es una dependencia legítima.
- **Tipos ancla `internal`**: si algún tipo elegido no es público, usar marcadores `public` en `src/`
  (alineado con las instrucciones) en lugar de exponer internals.
- **Demo (`demo/`)**: queda fuera del alcance de estos tests (es app de ejemplo). Opcionalmente se
  podría añadir un segundo bloque para validar las capas de `Raftel.Demo.*`, pero no es prioritario.
- **CI**: estos tests corren con `dotnet test` estándar; no requieren contenedores ni BD, por lo que
  son rápidos y aptos para cada pull request.

## 8. Criterios de aceptación (Definition of Done)

- [ ] `NetArchTest.Rules` añadido a `Directory.Packages.props`.
- [ ] Proyecto `tests/Raftel.ArchitectureTests` creado y referenciado en `Raftel.sln`.
- [ ] `ArchitectureTests.cs` con tests para R1–R6 (R7 opcional), usando Shouldly.
- [ ] `dotnet build Raftel.sln` y `dotnet test` en verde.
- [ ] Verificación negativa realizada (un `using` prohibido hace fallar el test) y revertida.
- [ ] Commit siguiendo Conventional Commits (p. ej. `test: add architecture tests with NetArchTest`).

## 9. Estimación

| Tarea | Esfuerzo |
|-------|----------|
| Paquete + proyecto + sln | ~20 min |
| `ArchitectureTests.cs` (R1–R6) | ~30 min |
| Verificación negativa + build/test | ~15 min |
| **Total** | **~1 h** |
