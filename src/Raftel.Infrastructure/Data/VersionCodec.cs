using System.Globalization;

namespace Raftel.Infrastructure.Data;

/// <summary>
/// Converts the provider-specific concurrency token value to and from the opaque string clients see:
/// Base64 of the 8 <c>rowversion</c> bytes on SQL Server, the decimal <c>xmin</c> on PostgreSQL.
/// </summary>
internal static class VersionCodec
{
    private const int RowVersionLength = 8;

    public static string Encode(object raw) => raw switch
    {
        byte[] bytes => Convert.ToBase64String(bytes),
        uint number => number.ToString(CultureInfo.InvariantCulture),
        _ => null
    };

    public static bool TryDecode(string version, Type tokenType, out object raw)
    {
        raw = null;

        if (tokenType == typeof(byte[]))
        {
            var bytes = new byte[RowVersionLength + 1];
            if (!Convert.TryFromBase64String(version, bytes, out var written) || written != RowVersionLength)
            {
                return false;
            }

            raw = bytes[..RowVersionLength];
            return true;
        }

        if (tokenType == typeof(uint) &&
            uint.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            raw = number;
            return true;
        }

        return false;
    }
}
