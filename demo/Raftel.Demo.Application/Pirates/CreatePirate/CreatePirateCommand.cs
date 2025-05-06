using Raftel.Application.Commands;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

public record CreatePirateCommand(string Name, uint Bounty, bool IsKing = false) : ICommand ;