using Raftel.Application.Authorization;
using Raftel.Application.Commands;
using Raftel.Demo.Application.Pirates;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

[RequiresPermission(PiratesPermissions.Management)]
public record CreatePirateCommand(string Name, uint Bounty, bool IsKing = false) : ICommand;