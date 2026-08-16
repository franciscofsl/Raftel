# 20 — Localización de mensajes de error

> Estado: **Pendiente**
> Ámbito: `Raftel.Domain`, `Raftel.Application`, `Raftel.Api.Server`
> Rompe API pública: no (aditivo)
> Depende de: [01](01-error-tipado-y-mapeo-http.md), [06](06-result-en-lugar-de-excepciones.md)

## 1. Motivación

Los mensajes de error están escritos a mano en inglés dentro del código:

```csharp
public static Error NullValue = new("Error.NullValue", "Null value was provided");
```

Los `*Errors.cs` (`UserErrors`, `TenantErrors`, `RoleErrors`) siguen el mismo
patrón. Una API que sirva a usuarios en otro idioma no tiene por dónde empezar:
el mensaje viaja ya formateado en la respuesta.

El proyecto es de un desarrollador hispanohablante y la documentación de
planificación está en español; el caso de uso es inmediato.

Existe además un problema de diseño anterior: los mensajes de `Error` mezclan
dos audiencias — el desarrollador (diagnóstico) y el usuario final (texto de
interfaz). No tienen por qué ser el mismo texto.

## 2. Diseño

### 2.1 El código es el contrato, el mensaje es presentación

Principio: **`Error.Code` es lo estable y lo que los clientes deben usar**;
`Error.Message` es un texto de conveniencia que puede cambiar y traducirse.

Esto ya se apoya en [01](01-error-tipado-y-mapeo-http.md), que expone `code` como
extensión de `ProblemDetails`. Reforzarlo en la documentación: un cliente que
haga `if (error.Message == "...")` está roto por diseño.

### 2.2 `IErrorLocalizer`

```csharp
// Raftel.Application/Abstractions/Localization/IErrorLocalizer.cs
public interface IErrorLocalizer
{
    string Localize(Error error, CultureInfo? culture = null);
}
```

Resolución en cascada:

1. Recurso con clave exacta `Error.Code` en la cultura solicitada.
2. Recurso en la cultura neutra del idioma (`es` si falla `es-ES`).
3. Recurso en la cultura por defecto configurada.
4. `Error.Message` tal cual (fallback siempre disponible: nunca se devuelve una
   cadena vacía ni la clave cruda).

Implementación sobre `IStringLocalizer` de
`Microsoft.Extensions.Localization` (framework compartido, sin dependencia nueva)
más una implementación `EmbeddedResourceErrorLocalizer` por si el consumidor no
quiere el sistema de localización de ASP.NET.

### 2.3 Parámetros en los mensajes

Muchos mensajes necesitan datos: *"El tenant 'Acme' ya existe"*. Hoy se
concatenan en el mensaje, lo que impide traducir. Solución: parámetros con nombre.

```csharp
public record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public IReadOnlyDictionary<string, object?> Parameters { get; init; }
        = new Dictionary<string, object?>();

    public Error WithParameters(params (string Key, object? Value)[] parameters);
}
```

Recurso: `Tenants.AlreadyExists = "Ya existe un tenant con el código {code}."`

El localizador sustituye por nombre, no por posición (el orden de los argumentos
cambia entre idiomas).

> **Seguridad**: los parámetros pueden contener entrada del usuario. Se
> sustituyen como texto plano; el recurso **nunca** se interpreta como formato
> compuesto arbitrario ni como HTML. Y los valores de parámetro no deben incluir
> datos sensibles: se documenta la regla y se revisa en los `*Errors.cs`.

### 2.4 Recursos incluidos

El framework aporta traducciones de sus propios errores en:

- `en` (neutra, por defecto)
- `es`

Archivos `.resx` en `Raftel.Application/Resources/RaftelErrors.{culture}.resx`.
El consumidor añade las suyas para sus códigos, y puede sobrescribir las del
framework registrando su propio localizador con mayor prioridad.

### 2.5 Negociación de cultura

`RequestLocalizationMiddleware` de ASP.NET Core con:

1. Cabecera `Accept-Language` (estándar).
2. Query string `?culture=es` (útil para pruebas).
3. Cultura del usuario si está almacenada en su perfil.
4. Cultura por defecto.

Lista blanca de culturas soportadas: una cultura arbitraria del cliente no debe
provocar búsquedas de recursos ilimitadas.

### 2.6 Dónde se traduce

**En el borde, no en el dominio.** `Raftel.Domain` sigue produciendo `Error` con
código, mensaje neutro y parámetros. La traducción ocurre en
`ErrorResults.ToProblem` (`Raftel.Api.Server`), justo antes de serializar.

Así:
- El dominio no depende de localización.
- Los logs conservan el mensaje neutro (buscar en logs por texto sigue funcionando
  independientemente del idioma del usuario) — decisión deliberada:
  **se loguea en la cultura neutra, se responde en la del usuario**.

### 2.7 Validadores

`Validator<T>` y `ValidationRule` producen errores con código; los mensajes de
validación se traducen por el mismo camino. Convención de códigos ya introducida
en [06](06-result-en-lugar-de-excepciones.md) (`"Email.Invalid"`), que encaja
directamente como clave de recurso.

## 3. Archivos a tocar

**Nuevos**
- `src/Raftel.Application/Abstractions/Localization/IErrorLocalizer.cs`
- `src/Raftel.Infrastructure/Localization/ErrorLocalizer.cs`
- `src/Raftel.Application/Resources/RaftelErrors.resx`, `RaftelErrors.es.resx`
- `src/Raftel.Application/LocalizationOptions.cs`
- `tests/Raftel.Application.UnitTests/Localization/ErrorLocalizerTests.cs`
- `tests/Raftel.Api.FunctionalTests/LocalizationTests.cs`

**Modificados**
- `src/Raftel.Domain/Abstractions/Error.cs` — `Parameters`
- `src/Raftel.Domain/Features/**/*Errors.cs` — parámetros en vez de concatenación
- `src/Raftel.Api.Server/AutoEndpoints/ErrorResults.cs`
- `src/Raftel.Api.Server/Middlewares/ExceptionHandlingMiddleware.cs`
- `src/Raftel.Infrastructure/DependencyInjection.cs`

## 4. Plan de implementación

1. **(test)** `Error.WithParameters` conserva igualdad por valor y no muta el original.
2. Añadir `Parameters` a `Error`.
3. **(test)** Cascada de resolución: cultura exacta, neutra, por defecto, fallback
   al `Message`.
4. Implementar `ErrorLocalizer`.
5. **(test)** Sustitución por nombre, con parámetros en orden distinto al del
   recurso.
6. **(test de seguridad)** Un parámetro con `{` o marcado tipo formato no rompe
   ni ejecuta nada.
7. Migrar los `*Errors.cs` del framework a mensajes parametrizados.
8. Crear los `.resx` `en` y `es`.
9. **(test funcional)** `Accept-Language: es` ⇒ `ProblemDetails.detail` en
   español, `code` idéntico.
10. **(test)** Los logs siguen en cultura neutra.
11. Documentar cómo añadir idiomas y sobrescribir mensajes del framework.

## 5. Tests

- **Unit**: cascada de resolución, parámetros, fallback.
- **Functional**: negociación por cabecera y query, cultura no soportada ⇒
  por defecto.
- **Consistencia**: test que recorre por reflexión todos los códigos de error
  declarados en `*Errors.cs` y verifica que **existe recurso en todas las
  culturas soportadas**. Evita traducciones a medias.

## 6. Criterios de aceptación

- [ ] `Error.Code` nunca cambia con el idioma.
- [ ] Todo código de error del framework tiene recurso en `en` y `es`.
- [ ] Los parámetros se sustituyen por nombre.
- [ ] Los logs permanecen en cultura neutra.
- [ ] Una cultura no soportada cae en la por defecto sin error.
- [ ] `Raftel.Domain` sigue sin dependencias de localización.

## 7. Alternativas descartadas

- **Traducir dentro del dominio**: metería `CultureInfo` e `IStringLocalizer` en
  la capa que debe estar libre de infraestructura.
- **Enviar solo el código y que traduzca el cliente**: correcto para SPAs, pero
  deja sin mensaje útil a clientes simples y a las herramientas de diagnóstico.
  El enfoque elegido permite ambas cosas: código estable + mensaje traducido.
- **Archivos JSON propios en vez de `.resx`**: `.resx` tiene soporte nativo de
  herramientas y de la cascada de culturas; no hay motivo para reinventarlo.
