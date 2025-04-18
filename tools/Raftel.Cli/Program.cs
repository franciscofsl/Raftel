using Raftel.Cli.Commands;
using Raftel.Cli.Commands.Add;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("raftel");
    config.AddCommand<CleanBuildFoldersCommand>("clean")
        .WithDescription("Removes bin and obj folders from the current directory recursively");
    
    config.AddBranch("add", add =>
    {
        add.AddCommand<AddCommandCommand>("command")
            .WithDescription("Generates a command with handler and validator"); 
        add.AddCommand<AddQueryCommand>("query")
            .WithDescription("Generates a query with handler and response"); 
    });
});

return app.Run(args);