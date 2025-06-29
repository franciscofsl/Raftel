namespace Raftel.Application.Features.Audit;

/// <summary>
/// Data transfer object for audit entries.
/// </summary>
public sealed record AuditEntryDto
{
    public required DateTime Date { get; init; }
    public required string ChangeType { get; init; }
    public required string EntityName { get; init; }
    public required string EntityId { get; init; }
    public string? Details { get; init; }
    public List<AuditPropertyChangeDto> Changes { get; init; } = new();
    public List<AuditPropertyChangeDto>? ChildChanges { get; init; }
}

/// <summary>
/// Data transfer object for audit property changes.
/// </summary>
public sealed record AuditPropertyChangeDto
{
    public required string PropertyName { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}