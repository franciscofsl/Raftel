using Raftel.Application.Authorization;
using Raftel.Application.Commands;

namespace Raftel.Application.UnitTests.Abstractions;

[RequiresPermission(ProtectedTestCommand.Permission)]
public sealed record ProtectedTestCommand : ICommand
{
    public const string Permission = "test.protected";
}
