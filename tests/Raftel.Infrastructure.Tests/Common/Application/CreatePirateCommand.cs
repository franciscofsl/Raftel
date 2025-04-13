using Raftel.Application.Commands;

namespace Raftel.Infrastructure.Tests.Common.Application;

public sealed record CreatePirateCommand(string Name, int Bounty) : ICommand;