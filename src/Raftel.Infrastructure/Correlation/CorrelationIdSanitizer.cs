namespace Raftel.Infrastructure.Correlation;

/// <summary>
/// Validates an incoming <c>X-Correlation-Id</c> header value against an allow-list of characters,
/// so that untrusted client input can never be used to inject or forge log lines.
/// </summary>
internal static class CorrelationIdSanitizer
{
    private const int MaxLength = 128;

    /// <summary>
    /// Determines whether <paramref name="value"/> is a safe correlation id: non-empty, at most
    /// <see cref="MaxLength"/> characters, and containing only letters, digits, hyphens, or underscores.
    /// </summary>
    /// <param name="value">The raw header value to validate.</param>
    /// <returns><see langword="true"/> if the value is safe to use as-is; otherwise <see langword="false"/>.</returns>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')
            {
                return false;
            }
        }

        return true;
    }
}
