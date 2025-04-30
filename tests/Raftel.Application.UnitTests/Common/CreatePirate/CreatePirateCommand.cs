using Raftel.Application.Commands;

namespace Raftel.Application.UnitTests.Common.CreatePirate;

public record CreatePirateCommand(string Name, int Bounty, bool IsKing = false) : ICommand ;