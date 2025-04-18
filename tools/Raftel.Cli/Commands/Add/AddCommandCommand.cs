using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;
using Spectre.Console.Cli;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using ICommand = Raftel.Application.Commands.ICommand;

namespace Raftel.Cli.Commands.Add;

internal sealed class AddCommandCommand : AsyncCommand<AddCommandCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<CommandName>")] public string CommandName { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var commandName = settings.CommandName;
        var folder = Path.Combine(Directory.GetCurrentDirectory(), commandName);
        Directory.CreateDirectory(folder);

        var commandPath = Path.Combine(folder, $"{commandName}Command.cs");
        var validatorPath = Path.Combine(folder, $"{commandName}CommandValidator.cs");
        var handlerPath = Path.Combine(folder, $"{commandName}CommandHandler.cs");
        var targetNamespace = NamespaceCalculator.FromPath(folder);

        await File.WriteAllTextAsync(commandPath, GenerateCommandCode(commandName, targetNamespace));
        await File.WriteAllTextAsync(validatorPath, GenerateValidatorCode(commandName, targetNamespace));
        await File.WriteAllTextAsync(handlerPath, GenerateHandlerCode(commandName, targetNamespace));

        AnsiConsole.MarkupLine($"[green]Generated command '{commandName}'[/]");
        return 0;
    }

    private string GenerateCommandCode(string name, string ns)
    {
        var recordDeclaration = RecordDeclaration(Token(SyntaxKind.RecordKeyword), $"{name}Command")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .WithParameterList(ParameterList())
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName(nameof(ICommand))))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(TriviaList(CarriageReturnLineFeed));

        var namespaceDeclaration = FileScopedNamespaceDeclaration(ParseName(ns))
            .AddMembers(recordDeclaration);

        var compilationUnit = CompilationUnit()
            .AddUsings(UsingDirective(ParseName(typeof(ICommand).Namespace)))
            .AddMembers(namespaceDeclaration);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    private string GenerateHandlerCode(string name, string ns)
    {
        var handlerClass = ClassDeclaration($"{name}CommandHandler")
            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
            .AddBaseListTypes(
                SimpleBaseType(ParseTypeName($"ICommandHandler<{name}Command>"))
            )
            .AddMembers(
                MethodDeclaration(ParseTypeName("Task<Result>"), "HandleAsync")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(
                        ParameterList(SingletonSeparatedList(
                            Parameter(Identifier("request")).WithType(ParseTypeName($"{name}Command"))
                        ))
                    )
                    .WithBody(
                        Block(
                            SingletonList<StatementSyntax>(
                                ThrowStatement(
                                    ObjectCreationExpression(ParseTypeName(nameof(NotImplementedException)))
                                        .WithArgumentList(ArgumentList())
                                )
                            )
                        )
                    )
            )
            .WithLeadingTrivia(TriviaList(CarriageReturnLineFeed));

        var namespaceDeclaration = FileScopedNamespaceDeclaration(ParseName(ns))
            .AddMembers(handlerClass);

        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("Raftel.Application.Commands")),
                UsingDirective(ParseName("Raftel.Domain.Abstractions"))
            )
            .AddMembers(namespaceDeclaration);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    private string GenerateValidatorCode(string name, string ns)
    {
        var constructor = ConstructorDeclaration($"{name}CommandValidator")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithBody(Block()); // constructor vacío

        var classDeclaration = ClassDeclaration($"{name}CommandValidator")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .AddBaseListTypes(
                SimpleBaseType(ParseTypeName($"Validator<{name}Command>"))
            )
            .AddMembers(constructor)
            .WithLeadingTrivia(TriviaList(CarriageReturnLineFeed));

        var namespaceDeclaration = FileScopedNamespaceDeclaration(ParseName(ns))
            .AddMembers(classDeclaration);

        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("Raftel.Domain.Validators"))
            )
            .AddMembers(namespaceDeclaration);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }
}