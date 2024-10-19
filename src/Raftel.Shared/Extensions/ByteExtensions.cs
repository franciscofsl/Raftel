namespace Raftel.Shared.Extensions;

public static class ByteExtensions
{
    public static string SizeInFile(this byte[] fileBytes)
    {
        if (fileBytes == null)
        {
            throw new ArgumentNullException(nameof(fileBytes), "File content cannot be null.");
        }

        string[] sizeUnits = ["bytes", "KB", "MB", "GB", "TB"];
        long fileSizeInBytes = fileBytes.Length;
        var size = fileSizeInBytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < sizeUnits.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:F2} {sizeUnits[unitIndex]}";
    }
}