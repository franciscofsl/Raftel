namespace Raftel.Domain.Abstractions;

/// <summary>
/// Represents a single client-requested sort field and direction.
/// A leading <c>-</c> on the field name indicates descending order (e.g. "-createdAt").
/// </summary>
/// <param name="Field">The name of the field to sort by.</param>
/// <param name="Direction">The direction to sort in.</param>
public sealed record SortRequest(string Field, SortDirection Direction = SortDirection.Ascending)
{
    /// <summary>
    /// Parses a comma-separated sort string into an ordered list of sort requests.
    /// </summary>
    /// <param name="raw">The raw sort string (e.g. "name,-createdAt"), or null/empty for no sort.</param>
    /// <returns>A successful result with the parsed sort requests, or a validation failure.</returns>
    public static Result<IReadOnlyList<SortRequest>> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Success<IReadOnlyList<SortRequest>>([]);
        }

        var segments = raw.Split(',');
        var sortRequests = new List<SortRequest>(segments.Length);

        foreach (var segment in segments)
        {
            var parsed = ParseSegment(segment);
            if (parsed.IsFailure)
            {
                return Result.Failure<IReadOnlyList<SortRequest>>(parsed.Error);
            }

            sortRequests.Add(parsed.Value);
        }

        return Result.Success<IReadOnlyList<SortRequest>>(sortRequests);
    }

    private static Result<SortRequest> ParseSegment(string segment)
    {
        var trimmed = segment.Trim();
        var isDescending = trimmed.StartsWith('-');
        var field = isDescending ? trimmed[1..] : trimmed;

        if (string.IsNullOrWhiteSpace(field))
        {
            return Result.Failure<SortRequest>(Error.Validation("Sort.Invalid", $"Invalid sort segment '{segment}'."));
        }

        var direction = isDescending ? SortDirection.Descending : SortDirection.Ascending;
        return Result.Success(new SortRequest(field, direction));
    }
}
