using System.Globalization;

namespace Raftel.Infrastructure.Data.Auditing;

/// <summary>
/// Default <see cref="IAuditValueSerializer"/> implementation. Relies on <c>ToString()</c>
/// to remain generic across any value object: Raftel value objects either override
/// <c>ToString()</c> explicitly (e.g. <c>Email</c>, <c>Bounty</c>, typed IDs) or are
/// <c>record</c> types, whose compiler-generated <c>ToString()</c> already renders nested
/// value objects recursively.
/// </summary>
internal sealed class AuditValueSerializer : IAuditValueSerializer
{
    public string? Serialize(object? value)
    {
        return value switch
        {
            null => null,
            string stringValue => stringValue,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
