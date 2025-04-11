using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("raftel-tools");
    config.AddCommand<GenerateTocCommand>("docfx generate-toc")
        .WithDescription("Genera un toc.yml agrupado por ensamblado desde los archivos .yml generados por DocFX");
});

return app.Run(args);