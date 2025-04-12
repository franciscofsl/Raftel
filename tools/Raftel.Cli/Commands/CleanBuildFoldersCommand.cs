using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Raftel.Cli.Commands;

public class CleanBuildFoldersCommand : Command
{
    public override int Execute([NotNull] CommandContext context)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var foldersToDelete = Directory
            .EnumerateDirectories(currentDirectory, "*", SearchOption.AllDirectories)
            .Where(d => d.EndsWith(Path.DirectorySeparatorChar + "bin") ||
                        d.EndsWith(Path.DirectorySeparatorChar + "obj"))
            .ToList();

        foreach (var folder in foldersToDelete)
        {
            try
            {
                Directory.Delete(folder, true);
                AnsiConsole.MarkupLine($"[green]✔ Deleted:[/] {folder}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✖ Error removing {folder}:[/] {ex.Message}");
            }
        }

        AnsiConsole.MarkupLine($"[blue]Total deleted folder: {foldersToDelete.Count}[/]");
        return CliResult.Success;
    }
}