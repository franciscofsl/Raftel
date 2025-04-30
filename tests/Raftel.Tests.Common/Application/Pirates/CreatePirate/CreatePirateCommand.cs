using Raftel.Application.Commands;

namespace Raftel.Tests.Common.Application.Pirates.CreatePirate;

public record CreatePirateCommand(string Name, int Bounty, bool IsKing = false) : ICommand ;