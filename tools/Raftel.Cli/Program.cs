using Raftel.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("raftel");
    config.AddCommand<CleanBuildFoldersCommand>("clean")
        .WithDescription("Removes bin and obj folders from the current directory recursively");

});

return app.Run(args);