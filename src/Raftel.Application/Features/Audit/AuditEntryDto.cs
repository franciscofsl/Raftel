namespace Raftel.Application.Features.Audit;

/// <summary>
/// Data transfer object for audit entries.
/// </summary>
public sealed record AuditEntryDto
{
    public required DateTime Fecha { get; init; }
    public required string TipoCambio { get; init; }
    public required string Entidad { get; init; }
    public required string IdEntidad { get; init; }
    public string? Detalle { get; init; }
    public List<AuditPropertyChangeDto> Cambios { get; init; } = new();
    public List<AuditPropertyChangeDto>? CambiosHija { get; init; }
}

/// <summary>
/// Data transfer object for audit property changes.
/// </summary>
public sealed record AuditPropertyChangeDto
{
    public required string Propiedad { get; init; }
    public string? Antes { get; init; }
    public string? Despues { get; init; }
}