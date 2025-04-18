using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;
using Spectre.Console.Cli;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Raftel.Cli.Commands.Add;

internal sealed class AddQueryCommand : AsyncCommand<AddQueryCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<QueryName>")] public string QueryName { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var name = settings.QueryName;
        var responseType = $"{name}Response";
        var folder = Path.Combine(Directory.GetCurrentDirectory(), name);
        Directory.CreateDirectory(folder);

        var targetNamespace = NamespaceCalculator.FromPath(folder);

        await File.WriteAllTextAsync(Path.Combine(folder, $"{name}Query.cs"),
            GenerateQueryCode(name, targetNamespace, responseType));
        await File.WriteAllTextAsync(Path.Combine(folder, $"{name}QueryHandler.cs"),
            GenerateHandlerCode(name, targetNamespace, responseType));
        await File.WriteAllTextAsync(Path.Combine(folder, $"{responseType}.cs"),
            GenerateResponseCode(responseType, targetNamespace));

        AnsiConsole.MarkupLine($"[green]Generated query '{settings.QueryName}'[/]");
        return CliResult.Success;
    }

    private string GenerateQueryCode(string name, string ns, string responseType)
    {
        var recordDeclaration = RecordDeclaration(Token(SyntaxKind.RecordKeyword), $"{name}Query")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .WithParameterList(ParameterList())
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName($"IQuery<{responseType}>")))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(TriviaList(CarriageReturnLineFeed));

        var namespaceDeclaration = FileScopedNamespaceDeclaration(ParseName(ns))
            .AddMembers(recordDeclaration);

        var compilationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("Raftel.Application.Queries")),
                UsingDirective(ParseName("Raftel.Domain.Abstractions"))
            )
            .AddMembers(namespaceDeclaration);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    private string GenerateHandlerCode(string name, string ns, string responseType)
    {
        var handlerClass = ClassDeclaration($"{name}QueryHandler")
            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
            .AddBaseListTypes(
                SimpleBaseType(ParseTypeName($"IQueryHandler<{name}Query, {responseType}>"))
            )
            .AddMembers(
                MethodDeclaration(ParseTypeName($"Task<Result<{responseType}>>"), "HandleAsync")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithParameterList(
                        ParameterList(SingletonSeparatedList(
                            Parameter(Identifier("request")).WithType(ParseTypeName($"{name}Query"))
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
                UsingDirective(ParseName("Raftel.Application.Queries")),
                UsingDirective(ParseName("Raftel.Domain.Abstractions"))
            )
            .AddMembers(namespaceDeclaration);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    private string GenerateResponseCode(string responseName, string ns)
    {
        var classDeclaration = ClassDeclaration(responseName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .WithLeadingTrivia(TriviaList(CarriageReturnLineFeed))
            .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
            .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken));

        var namespaceDeclaration = FileScopedNamespaceDeclaration(ParseName(ns))
            .AddMembers(classDeclaration);

        var compilationUnit = CompilationUnit()
            .AddMembers(namespaceDeclaration);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }
}