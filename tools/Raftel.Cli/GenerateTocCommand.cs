using System.ComponentModel;
using YamlDotNet.Serialization.NamingConventions;

using Spectre.Console.Cli;
using System.ComponentModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class GenerateTocCommand : Command<GenerateTocCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Ruta a la carpeta donde están los .yml (por defecto: ./api)")]
        [CommandOption("-p|--path")]
        public string Path { get; set; } = "api";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var apiDir = Path.GetFullPath(settings.Path);
        var tocPath = System.IO.Path.Combine(apiDir, "toc.yml");

        var assemblies = new Dictionary<string, List<(string uid, string name)>>();

        foreach (var file in Directory.EnumerateFiles(apiDir, "*.yml"))
        {
            var lines = File.ReadAllLines(file);

            string? uid = null;
            string? name = null;
            string? assembly = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("uid: "))
                    uid = line.Replace("uid: ", "").Trim();

                if (line.StartsWith("name: ") && name == null)
                    name = line.Replace("name: ", "").Trim();

                if (line.StartsWith("assembly: "))
                    assembly = line.Replace("assembly: ", "").Trim();

                if (uid != null && name != null && assembly != null)
                    break;
            }

            if (uid != null && name != null && assembly != null)
            {
                if (!assemblies.ContainsKey(assembly))
                    assemblies[assembly] = new List<(string uid, string name)>();

                assemblies[assembly].Add((uid, name));
            }
        }

        var items = new List<object>();

        foreach (var kvp in assemblies.OrderBy(k => k.Key))
        {
            var children = kvp.Value.OrderBy(e => e.name)
                .Select(e => new Dictionary<string, string>
                {
                    { "uid", e.uid },
                    { "name", e.name }
                }).ToList();

            items.Add(new Dictionary<string, object>
            {
                { "name", kvp.Key },
                { "items", children }
            });
        }

        var yamlObject = new Dictionary<string, object>
        {
            { "### YamlMime", "TableOfContent" },
            { "items", items }
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance) // Preserve original casing
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build(); 
         

        var yaml = serializer.Serialize(yamlObject);
        File.WriteAllText(tocPath, yaml);

        Console.WriteLine($"[✔] TOC generado en: {tocPath}");
        return 0;
    }
}