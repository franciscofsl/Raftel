# Sistema de Auditoría de Cambios - Raftel

## Descripción

Este sistema implementa una solución completa de auditoría de cambios para entidades utilizando Entity Framework Core, siguiendo los principios de arquitectura limpia.

## Características

- **Auditoría automática**: Captura automáticamente cambios en entidades configuradas
- **Tipos de cambios**: Crear, Editar, Eliminar (soft delete)
- **Configuración flexible**: Permite incluir/excluir propiedades específicas
- **Consultas avanzadas**: Query para obtener historial de cambios
- **Arquitectura limpia**: El dominio no tiene conocimiento de la auditoría

## Configuración

### 1. Registrar el sistema de auditoría

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRaftelData<MyDbContext>(configuration, configureAudit: options =>
    {
        // Configurar auditoría para entidades específicas
        options.Add<Pedido>()
            .WithCreation()
            .WithUpdate()
            .WithDeletion()
            .ExcludeProperties("CreatedAt", "UpdatedAt");

        options.Add<DetallePedido>()
            .WithCreation()
            .WithUpdate()
            .IncludeOnlyProperties("Cantidad", "Precio");
    });
}
```

### 2. Configuración avanzada de entidades

```csharp
// Auditar solo creación y actualización
options.Add<Cliente>()
    .WithCreation()
    .WithUpdate()
    .WithDeletion(false);

// Auditar solo propiedades específicas
options.Add<Producto>()
    .IncludeOnlyProperties("Nombre", "Precio", "Stock");

// Excluir propiedades sensibles
options.Add<Usuario>()
    .ExcludeProperties("Password", "Salt", "Token");
```

## Uso

### Consultar historial de auditoría

```csharp
public class PedidoController : ControllerBase
{
    private readonly IQueryDispatcher _queryDispatcher;

    public PedidoController(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
    }

    [HttpGet("{id}/audit")]
    public async Task<IActionResult> GetAuditHistory(string id)
    {
        var query = new GetEntityAuditHistoryQuery("Pedido", id);
        var result = await _queryDispatcher.DispatchAsync(query);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Error);
    }
}
```

### Ejemplo de respuesta JSON

```json
[
  {
    "fecha": "2025-01-15T09:00:00Z",
    "tipoCambio": "Crear",
    "entidad": "Pedido",
    "idEntidad": "123",
    "cambios": [
      {"propiedad": "Estado", "antes": null, "despues": "Nuevo"},
      {"propiedad": "Total", "antes": null, "despues": "150.00"}
    ]
  },
  {
    "fecha": "2025-01-15T10:30:00Z",
    "tipoCambio": "Editar",
    "entidad": "Pedido",
    "idEntidad": "123",
    "cambios": [
      {"propiedad": "Estado", "antes": "Nuevo", "despues": "Procesando"}
    ]
  },
  {
    "fecha": "2025-01-15T11:00:00Z",
    "tipoCambio": "AñadirEntidadHija",
    "entidad": "Pedido",
    "idEntidad": "123",
    "detalle": "Añadido DetallePedido con id 456",
    "cambiosHija": [
      {"propiedad": "Cantidad", "antes": null, "despues": "2"},
      {"propiedad": "Precio", "antes": null, "despues": "75.00"}
    ]
  }
]
```

## Estructura de Base de Datos

### Tabla AuditEntries
- `Id` (uniqueidentifier): Identificador único del registro de auditoría
- `Timestamp` (datetime2): Fecha y hora del cambio
- `ChangeType` (nvarchar(50)): Tipo de cambio (Crear, Editar, Eliminar, etc.)
- `EntityName` (nvarchar(250)): Nombre de la entidad
- `EntityId` (nvarchar(450)): Identificador de la entidad
- `Details` (nvarchar(2000)): Detalles adicionales del cambio

### Tabla AuditPropertyChanges
- `Id` (uniqueidentifier): Identificador único del cambio de propiedad
- `AuditEntryId` (uniqueidentifier): Referencia al registro de auditoría
- `PropertyName` (nvarchar(250)): Nombre de la propiedad
- `OldValue` (nvarchar(max)): Valor anterior
- `NewValue` (nvarchar(max)): Valor nuevo

## Consideraciones de Rendimiento

- Los registros de auditoría se crean en la misma transacción
- Para alta concurrencia, considerar implementar colas asíncronas
- Los índices están optimizados para consultas por entidad y fecha
- Los valores se serializan como JSON para tipos complejos

## Extensibilidad

El sistema está diseñado para ser extensible:

- Nuevos tipos de cambios pueden agregarse a `AuditChangeType`
- Interceptores adicionales pueden capturar eventos específicos
- Filtros personalizados pueden implementarse para reglas complejas
- Queries adicionales pueden agregarse para diferentes tipos de consultas

## Testing

El sistema incluye tests exhaustivos:

- **Unit Tests**: Para lógica de configuración y entidades
- **Integration Tests**: Para validar el flujo completo de auditoría
- **Functional Tests**: Para verificar la captura de cambios en escenarios reales

## Principios de Diseño

- **Arquitectura Limpia**: La auditoría está en la capa de infraestructura
- **Object Calisthenics**: Clases pequeñas con responsabilidades únicas
- **SOLID**: Cada componente tiene una responsabilidad específica
- **Testeable**: Todas las funcionalidades están cubiertas por tests