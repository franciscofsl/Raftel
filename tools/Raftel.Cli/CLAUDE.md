# Raftel.Cli

Command-line tool for framework **scaffolding**. Generates Commands/Queries and maintains the repo. Built with **Spectre.Console** (CLI UX) and **Microsoft.CodeAnalysis (Roslyn)** for code generation: emits syntax trees, not text templates, so generated code **always compiles syntactically**.

## Structure

```
Program.cs                       CLI entry point and command registration
CliResult.cs                     uniform result per command
NamespaceCalculator.cs           deduces target namespace from project path
Commands/
    Add/AddCommandCommand.cs     generates Command + Handler (+ Validator) in its feature
    Add/AddQueryCommand.cs       generates Query + Handler + Response
    CleanBuildFoldersCommand.cs  cleans bin/obj folders
```

## Patterns and Practices

- **Roslyn code generation**: when adding/editing a generator, build nodes with `Microsoft.CodeAnalysis.CSharp` API (`SyntaxFactory`, etc.) instead of string interpolation. Generated code must respect repo conventions: file-scoped namespaces, vertical feature (`Features/<Feature>/<UseCase>/`), records for Command/Query, `sealed` handler.
- **Commands**: one command per file under `Commands/`, returning `CliResult`. UX (prompts, tables, colors) via Spectre.Console.
- Generated code must fit `Raftel.Application` as described in its `CLAUDE.md`; if those conventions change, update generators here.

## Execution

```bash
dotnet run --project tools/Raftel.Cli -- <command> [options]
```
