namespace Raftel.Shared.Extensions;

public static class StringExtensions
{
    public static string ToCamelCase(this string input)
    {
        return string.IsNullOrEmpty(input) || char.IsLower(input[0])
            ? input
            : char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}