using Raftel.Application.Commands;

namespace Raftel.Demo.Application.Pirates.CreatePirate;

public record CreatePirateCommand(string Name, int Bounty, bool IsKing = false) : ICommand ;