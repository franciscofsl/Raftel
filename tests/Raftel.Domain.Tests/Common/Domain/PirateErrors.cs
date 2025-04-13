using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Tests.Common.Domain;

public static class PirateErrors
{
    public static Error LuffyShouldBeThePirateKing => new Error("Pirate.Name", "Luffy should be The Pirate King.");
}