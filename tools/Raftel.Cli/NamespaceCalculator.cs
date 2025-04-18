using System.Xml.Linq;

namespace Raftel.Cli;

internal static class NamespaceCalculator
{
    public static string FromPath(string targetPath)
    {
        var dir = new DirectoryInfo(targetPath);

        FileInfo? csprojFile = null;
        while (dir != null && csprojFile == null)
        {
            csprojFile = dir.GetFiles("*.csproj").FirstOrDefault();
            dir = dir.Parent;
        }

        if (csprojFile == null)
        {
            throw new InvalidOperationException("No .csproj file found in path hierarchy.");
        }

        var projectDirectory = csprojFile.Directory!;
        var projectName = Path.GetFileNameWithoutExtension(csprojFile.Name);

        var doc = XDocument.Load(csprojFile.FullName);
        var rootNs = doc.Descendants("RootNamespace").FirstOrDefault()?.Value ?? projectName;

        var relativePath = Path.GetRelativePath(projectDirectory.FullName, targetPath);
        var relativeParts = relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Replace(" ", "").Replace("-", ""))
            .ToArray();

        var finalNamespace = string.Join(".", new[] { rootNs }.Concat(relativeParts));

        return finalNamespace;
    }
}