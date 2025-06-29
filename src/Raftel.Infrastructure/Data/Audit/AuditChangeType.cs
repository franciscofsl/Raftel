namespace Raftel.Infrastructure.Data.Audit;

/// <summary>
/// Defines the types of changes that can be audited.
/// </summary>
public static class AuditChangeType
{
    public const string Create = "Crear";
    public const string Update = "Editar";
    public const string Delete = "Eliminar";
    public const string AddChildEntity = "AñadirEntidadHija";
    public const string RemoveChildEntity = "EliminarEntidadHija";
}