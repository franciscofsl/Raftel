using Raftel.Application.Abstractions;
using Raftel.Application.Commands;

namespace Raftel.Application.Tests.Common;

public record CreatePirateCommand(string Name, int Bounty, bool IsKing = false) : ICommand ;