using Raftel.Application.Commands;

namespace Raftel.Application.UnitTests.Abstractions;

public sealed record TestCommandWithResult(string Message) : ICommand<string>;
